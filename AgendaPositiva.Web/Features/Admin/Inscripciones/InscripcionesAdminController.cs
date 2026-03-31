using AgendaPositiva.Web.Datos;
using AgendaPositiva.Web.Features.Commons;
using AgendaPositiva.Web.Features.Commons.Views;
using AgendaPositiva.Web.Features.Admin.Auditoria;
using AgendaPositiva.Web.Features.Admin.Inscripciones.Views.ViewModels;
using AgendaPositiva.Web.Features.Inscripciones.Dominio;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace AgendaPositiva.Web.Features.Admin.Inscripciones;

[Route("admin/inscripciones")]
[Authorize(Policy = "AdminPanel")]
public class InscripcionesAdminController : Controller
{
    readonly AppDbContext store;
    readonly Evento evento;
    readonly UbicacionService ubicacionService;

    public InscripcionesAdminController(AppDbContext db, UbicacionService ubicacionService)
    {
        store = db;
        this.ubicacionService = ubicacionService;
        evento = store.Eventos.FirstOrDefault(e => e.Activo) ?? throw new Exception("No hay un evento activo");
    }

    bool EsAdministrador => User.IsInRole("Administrador");

    Dictionary<string, List<string>> LocalidadesAsignadas
    {
        get
        {
            var json = User.FindFirstValue("Localidades");
            if (string.IsNullOrEmpty(json)) return [];
            return JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json) ?? [];
        }
    }

    List<string> DepartamentosAsignados => [.. LocalidadesAsignadas.Keys];

    bool TieneAcceso(string departamento, string ciudad)
    {
        var localidades = LocalidadesAsignadas;
        if (!localidades.TryGetValue(departamento, out var ciudades)) return false;
        return ciudades.Count == 0 || ciudades.Contains(ciudad);
    }

    IQueryable<Inscripcion> FiltrarPorLocalidades(IQueryable<Inscripcion> query)
    {
        var localidades = LocalidadesAsignadas;
        var deptosCompletos = localidades.Where(kv => kv.Value.Count == 0).Select(kv => kv.Key).ToList();
        var deptosParciales = localidades.Where(kv => kv.Value.Count > 0).ToDictionary(kv => kv.Key, kv => kv.Value);

        if (deptosParciales.Count == 0)
        {
            return query.Where(i => deptosCompletos.Contains(i.Departamento));
        }

        var ciudadesPermitidas = deptosParciales.SelectMany(kv => kv.Value).ToList();
        var deptosParcKeys = deptosParciales.Keys.ToList();

        return query.Where(i =>
            deptosCompletos.Contains(i.Departamento) ||
            (deptosParcKeys.Contains(i.Departamento) && ciudadesPermitidas.Contains(i.Ciudad)));
    }

    [HttpGet]
    public IActionResult Index(
        [FromQuery] string? nombre,
        [FromQuery] string? departamento,
        [FromQuery] EstadoInscripcion? estado,
        [FromQuery] bool? hospedaje,
        [FromQuery] string? sortLocalidad)
    {
        var query = store.Inscripciones
            .Include(i => i.Persona)
            .Where(i => i.EventoId == evento.Id);

        // Colaboradores solo ven sus localidades asignadas
        if (!EsAdministrador)
        {
            query = FiltrarPorLocalidades(query);
        }

        if (!string.IsNullOrWhiteSpace(nombre))
        {
            var term = nombre.Trim().ToLower();
            query = query.Where(i =>
                (i.Persona.Nombres + " " + i.Persona.Apellidos).ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(departamento))
        {
            query = query.Where(i => i.Departamento == departamento);
        }

        if (estado.HasValue)
        {
            query = query.Where(i => i.Estado == estado.Value);
        }

        if (hospedaje.HasValue)
        {
            query = query.Where(i => i.RequiereHospedaje == hospedaje.Value);
        }

        var inscripciones = (sortLocalidad switch
        {
            "asc" => query.OrderBy(i => i.Departamento).ThenBy(i => i.Ciudad),
            "desc" => query.OrderByDescending(i => i.Departamento).ThenByDescending(i => i.Ciudad),
            _ => query.OrderByDescending(i => i.FechaCreacion)
        }).ToList();

        var vm = new ListaInscripcionesViewModel
        {
            Evento = evento,
            Inscripciones = inscripciones,
            Departamentos = EsAdministrador
                ? ubicacionService.ObtenerNombresDepartamentos()
                : DepartamentosAsignados,
            FiltroNombre = nombre,
            FiltroDepartamento = departamento,
            FiltroEstado = estado,
            FiltroHospedaje = hospedaje,
            SortLocalidad = sortLocalidad
        };

        return View("~/Features/Admin/Inscripciones/Views/Index.cshtml", vm);
    }

    [HttpGet("{id:int}")]
    public IActionResult Detalle(int id)
    {
        var inscripcion = store.Inscripciones
            .Include(i => i.Persona)
            .Include(i => i.GrupoFamiliar)
            .Include(i => i.RelacionConPersona)
            .FirstOrDefault(i => i.Id == id && i.EventoId == evento.Id);

        if (inscripcion is null) return NotFound();

        if (!EsAdministrador && !TieneAcceso(inscripcion.Departamento, inscripcion.Ciudad))
            return Forbid();

        ViewBag.Auditoria = store.AuditoriaAdmin
            .Where(a => a.InscripcionId == id)
            .OrderByDescending(a => a.FechaCreacion)
            .ToList();

        return View("~/Features/Admin/Inscripciones/Views/Detalle.cshtml", inscripcion);
    }

    [HttpPost("{id:int}/cambiar-estado")]
    public IActionResult CambiarEstado(int id, [FromForm] EstadoInscripcion nuevoEstado)
    {
        if (nuevoEstado != EstadoInscripcion.NoVaAsistir && nuevoEstado != EstadoInscripcion.Pendiente && nuevoEstado != EstadoInscripcion.PagoCompletado)
            return BadRequest("Estado no permitido.");

        var inscripcion = store.Inscripciones
            .FirstOrDefault(i => i.Id == id && i.EventoId == evento.Id);

        if (inscripcion is null) return NotFound();

        if (!EsAdministrador && !TieneAcceso(inscripcion.Departamento, inscripcion.Ciudad))
            return Forbid();

        var estadoAnterior = inscripcion.Estado;
        inscripcion.Estado = nuevoEstado;
        inscripcion.FechaActualizacion = DateTime.UtcNow;

        store.AuditoriaAdmin.Add(new AuditoriaAdmin
        {
            InscripcionId = id,
            Usuario = User.FindFirstValue(ClaimTypes.Name) ?? "Desconocido",
            Accion = "Cambio de estado",
            ValorAnterior = estadoAnterior.Humanize(),
            ValorNuevo = nuevoEstado.Humanize()
        });

        store.SaveChanges();

        return RedirectToAction(nameof(Detalle), new { id });
    }

    [HttpPost("{id:int}/cambiar-hospedaje")]
    public IActionResult CambiarHospedaje(int id, [FromForm] bool requiereHospedaje)
    {
        var inscripcion = store.Inscripciones
            .FirstOrDefault(i => i.Id == id && i.EventoId == evento.Id);

        if (inscripcion is null) return NotFound();

        if (!EsAdministrador && !TieneAcceso(inscripcion.Departamento, inscripcion.Ciudad))
            return Forbid();

        var valorAnterior = inscripcion.RequiereHospedaje;
        inscripcion.RequiereHospedaje = requiereHospedaje;
        inscripcion.FechaActualizacion = DateTime.UtcNow;

        store.AuditoriaAdmin.Add(new AuditoriaAdmin
        {
            InscripcionId = id,
            Usuario = User.FindFirstValue(ClaimTypes.Name) ?? "Desconocido",
            Accion = "Cambio de hospedaje",
            ValorAnterior = valorAnterior ? "Sí" : "No",
            ValorNuevo = requiereHospedaje ? "Sí" : "No"
        });

        store.SaveChanges();

        return RedirectToAction(nameof(Detalle), new { id });
    }

    [HttpGet("grupo/{grupoId:int}")]
    public IActionResult GrupoFamiliar(int grupoId)
    {
        var grupo = store.GrupoFamiliar
            .Include(g => g.Inscripciones)
                .ThenInclude(i => i.Persona)
            .Include(g => g.Inscripciones)
                .ThenInclude(i => i.RelacionConPersona)
            .FirstOrDefault(g => g.Id == grupoId);

        if (grupo is null) return NotFound();

        // Verificar que al menos una inscripción pertenece al evento activo
        var inscripcionesEvento = grupo.Inscripciones.Where(i => i.EventoId == evento.Id).ToList();
        if (inscripcionesEvento.Count == 0) return NotFound();

        if (!EsAdministrador)
        {
            if (!inscripcionesEvento.Any(i => TieneAcceso(i.Departamento, i.Ciudad)))
                return Forbid();
        }

        return View("~/Features/Admin/Inscripciones/Views/GrupoFamiliar.cshtml", grupo);
    }

    [HttpGet("exportar")]
    public IActionResult Exportar()
    {
        var query = store.Inscripciones
            .Include(i => i.Persona)
            .Include(i => i.GrupoFamiliar)
            .Where(i => i.EventoId == evento.Id);

        if (!EsAdministrador)
        {
            query = FiltrarPorLocalidades(query);
        }

        var inscripciones = query
            .OrderBy(i => i.Departamento)
            .ThenBy(i => i.Ciudad)
            .ThenBy(i => i.GrupoFamiliarId)
            .ToList();

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Inscripciones");

        // Headers
        var headers = new[] { "#", "Nombres", "Apellidos", "Género",
            "Tipo Identificación", "Número Identificación", "Fecha Nacimiento",
            "Teléfono", "Email", "Departamento", "Ciudad", "Estado", "Hospedaje",
            "Grupo Familiar", "Servicios", "Necesidades Especiales", "Fecha Registro" };

        for (int i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];

        var headerRow = ws.Range(1, 1, 1, headers.Length);
        headerRow.Style.Font.Bold = true;
        headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#1a1a2e");
        headerRow.Style.Font.FontColor = XLColor.White;


        // Data
        for (int i = 0; i < inscripciones.Count; i++)
        {
            var ins = inscripciones[i];
            string relacion = "";
            if (ins.Relacion.HasValue)
            {
                relacion = $"{ins.Relacion.Value.Humanize()} de {ins.RelacionConPersona?.NombreCompleto}";
            }
            int row = i + 2;
            ws.Cell(row, 1).Value = i + 1;
            ws.Cell(row, 3).Value = ins.Persona.Apellidos;
            ws.Cell(row, 2).Value = ins.Persona.Nombres;
            ws.Cell(row, 4).Value = ins.Persona.Genero.Humanize();
            ws.Cell(row, 5).Value = ins.Persona.TipoIdentificacion.Humanize();
            ws.Cell(row, 6).Value = ins.Persona.NumeroIdentificacion;
            ws.Cell(row, 7).Value = ins.Persona.FechaNacimiento.ToString("dd/MM/yyyy");
            ws.Cell(row, 8).Value = ins.Persona.Telefono;
            ws.Cell(row, 9).Value = ins.Persona.Email ?? "";
            ws.Cell(row, 10).Value = ins.Departamento;
            ws.Cell(row, 11).Value = ins.Ciudad;
            ws.Cell(row, 12).Value = ins.Estado.Humanize();
            ws.Cell(row, 13).Value = ins.RequiereHospedaje ? "Sí" : "No";
            ws.Cell(row, 14).Value = ins.GrupoFamiliar?.Id.ToString() ?? "N/A";
            ws.Cell(row, 15).Value = string.Join(", ", ins.Servicios.Select(s => s.Descripcion()));
            ws.Cell(row, 16).Value = ins.NecesidadesEspeciales ?? "";
            ws.Cell(row, 17).Value = ins.FechaCreacion.ToString("dd/MM/yyyy HH:mm");
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var fileName = $"Inscripciones_{evento.Nombre.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}.xlsx";
        return File(stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }
}
