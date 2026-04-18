using AgendaPositiva.Web.Datos;
using AgendaPositiva.Web.Features.Admin.Regiones.Dominio;
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

    List<int> RegionIdsAsignados
    {
        get
        {
            var json = User.FindFirstValue("RegionIds");
            if (string.IsNullOrEmpty(json)) return [];
            return JsonSerializer.Deserialize<List<int>>(json) ?? [];
        }
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

    async Task<(int Bebes, int Ninos, int Adolescentes, int Adultos)> ObtenerDesglosePorEdad(IQueryable<Inscripcion>? filtro = null)
    {
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var corteBebe = hoy.AddYears(-3);
        var corteNino = hoy.AddYears(-11);
        var corteAdolescente = hoy.AddYears(-18);

        var query = filtro ?? store.Inscripciones.Where(i => i.EventoId == evento.Id);
        var fechas = await query.Select(i => i.Persona.FechaNacimiento).ToListAsync();

        return (
            fechas.Count(f => f > corteBebe),
            fechas.Count(f => f <= corteBebe && f > corteNino),
            fechas.Count(f => f <= corteNino && f > corteAdolescente),
            fechas.Count(f => f <= corteAdolescente)
        );
    }

    async Task<CupoInfoViewModel> ObtenerCupoInfo()
    {
        if (EsAdministrador)
        {
            var desglose = await ObtenerDesglosePorEdad();
            // Administradores ven el cupo total del evento
            return new CupoInfoViewModel
            {
                NombreRegion = evento.Nombre,
                TotalInscritos = evento.TotalInscritos,
                CupoTotal = evento.CupoTotal,
                CupoDisponible = evento.CupoDisponible,
                TotalBebes = desglose.Bebes,
                TotalNinos = desglose.Ninos,
                TotalAdolescentes = desglose.Adolescentes,
                TotalAdultos = desglose.Adultos
            };
        }

        // Colaboradores ven el cupo agregado de sus regiones asignadas
        var regionIds = RegionIdsAsignados;
        if (regionIds.Count == 0)
        {
            return new CupoInfoViewModel
            {
                NombreRegion = "Sin regiones asignadas",
                TotalInscritos = 0,
                CupoTotal = 0,
                CupoDisponible = 0
            };
        }

        var regiones = await store.RegionesEvento
            .Where(r => regionIds.Contains(r.Id))
            .ToListAsync();

        var totalInscritos = regiones.Sum(r => r.TotalInscritos);
        var cupoTotal = regiones.Sum(r => r.Cupo);

        var desgloseColab = await ObtenerDesglosePorEdad(
            FiltrarPorLocalidades(store.Inscripciones.Where(i => i.EventoId == evento.Id)));

        return new CupoInfoViewModel
        {
            NombreRegion = regiones.Count == 1 ? regiones[0].Nombre : $"{regiones.Count} regiones",
            TotalInscritos = totalInscritos,
            CupoTotal = cupoTotal,
            CupoDisponible = cupoTotal - totalInscritos,
            Regiones = regiones,
            TotalBebes = desgloseColab.Bebes,
            TotalNinos = desgloseColab.Ninos,
            TotalAdolescentes = desgloseColab.Adolescentes,
            TotalAdultos = desgloseColab.Adultos
        };
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        [FromQuery] string? nombre,
        [FromQuery] string? documento,
        [FromQuery] string? departamento,
        [FromQuery] string? sortLocalidad,
        [FromQuery] int pagina = 1,
        [FromQuery] int porPagina = 50)
    {
        porPagina = porPagina is 10 or 50 or 100 ? porPagina : 50;

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

        if (!string.IsNullOrWhiteSpace(documento))
        {
            var term = documento.Trim();
            query = query.Where(i => i.Persona.NumeroIdentificacion.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(departamento))
        {
            query = query.Where(i => i.Departamento == departamento);
        }

        var orderedQuery = sortLocalidad switch
        {
            "asc" => query.OrderBy(i => i.Departamento).ThenBy(i => i.Ciudad),
            "desc" => query.OrderByDescending(i => i.Departamento).ThenByDescending(i => i.Ciudad),
            _ => query.OrderByDescending(i => i.FechaCreacion)
        };

        var total = orderedQuery.Count();
        var totalPaginas = (int)Math.Ceiling(total / (double)porPagina);
        if (pagina < 1) pagina = 1;
        if (pagina > totalPaginas && totalPaginas > 0) pagina = totalPaginas;

        var inscripciones = orderedQuery
            .Skip((pagina - 1) * porPagina)
            .Take(porPagina)
            .ToList();

        var vm = new ListaInscripcionesViewModel
        {
            Evento = evento,
            Inscripciones = inscripciones,
            TotalInscripciones = total,
            Departamentos = EsAdministrador
                ? ubicacionService.ObtenerNombresDepartamentos()
                : DepartamentosAsignados,
            FiltroNombre = nombre,
            FiltroDocumento = documento,
            FiltroDepartamento = departamento,
            SortLocalidad = sortLocalidad,
            Pagina = pagina,
            PorPagina = porPagina,
            TotalPaginas = totalPaginas,
            CupoInfo = await ObtenerCupoInfo(),
        };

        return View("~/Features/Admin/Inscripciones/Views/Index.cshtml", vm);
    }

    [HttpGet("{id:int}")]
    public IActionResult Detalle(int id)
    {
        var inscripcion = store.Inscripciones
            .Include(i => i.Persona)
            .Include(i => i.GrupoFamiliar)
                .ThenInclude(g => g!.Inscripciones)
                    .ThenInclude(i => i.Persona)
            .Include(i => i.GrupoFamiliar)
                .ThenInclude(g => g!.Inscripciones)
                    .ThenInclude(i => i.RelacionConPersona)
            .Include(i => i.RelacionConPersona)
            .FirstOrDefault(i => i.Id == id && i.EventoId == evento.Id);

        if (inscripcion is null) return NotFound();

        if (!EsAdministrador && !TieneAcceso(inscripcion.Departamento, inscripcion.Ciudad))
            return View("~/Features/Admin/Inscripciones/Views/SinAcceso.cshtml");

        var auditoria = store.AuditoriaAdmin
            .Where(a => a.InscripcionId == id)
            .OrderByDescending(a => a.FechaCreacion)
            .ToList();

        return View("~/Features/Admin/Inscripciones/Views/Detalle.cshtml", new Views.ViewModels.DetalleViewModel
        {
            Inscripcion = inscripcion,
            Auditoria = auditoria
        });
    }

    [HttpPost("{id:int}/cambiar-estado")]
    [Authorize(Roles = "Administrador")]
    public IActionResult CambiarEstado(int id, [FromForm] EstadoInscripcion nuevoEstado)
    {
        if (!Enum.IsDefined(nuevoEstado))
            return BadRequest("Estado no válido.");

        var inscripcion = store.Inscripciones
            .FirstOrDefault(i => i.Id == id && i.EventoId == evento.Id);

        if (inscripcion is null) return NotFound();

        if (!EsAdministrador && !TieneAcceso(inscripcion.Departamento, inscripcion.Ciudad))
            return View("~/Features/Admin/Inscripciones/Views/SinAcceso.cshtml");

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
    [Authorize(Roles = "Administrador")]
    public IActionResult CambiarHospedaje(int id, [FromForm] bool requiereHospedaje)
    {
        var inscripcion = store.Inscripciones
            .FirstOrDefault(i => i.Id == id && i.EventoId == evento.Id);

        if (inscripcion is null) return NotFound();

        if (!EsAdministrador && !TieneAcceso(inscripcion.Departamento, inscripcion.Ciudad))
            return View("~/Features/Admin/Inscripciones/Views/SinAcceso.cshtml");

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

    [HttpGet("{id:int}/editar")]
    [Authorize(Roles = "Administrador")]
    public IActionResult Editar(int id)
    {
        var inscripcion = store.Inscripciones
            .Include(i => i.Persona)
            .FirstOrDefault(i => i.Id == id && i.EventoId == evento.Id);

        if (inscripcion is null) return NotFound();

        return View("~/Features/Admin/Inscripciones/Views/Editar.cshtml",
            new Views.ViewModels.EditarInscripcionViewModel
            {
                Inscripcion = inscripcion,
                DepartamentosDisponibles = ubicacionService.ObtenerNombresDepartamentos()
            });
    }

    [HttpPost("{id:int}/editar")]
    [Authorize(Roles = "Administrador")]
    public IActionResult PostEditar(
        int id,
        [FromForm] string Nombres,
        [FromForm] string Apellidos,
        [FromForm] Genero Genero,
        [FromForm] DateOnly FechaNacimiento,
        [FromForm] string Telefono,
        [FromForm] string? Email,
        [FromForm] string Departamento,
        [FromForm] string Ciudad,
        [FromForm] bool RequiereHospedaje,
        [FromForm] bool RequiereAlimentacion,
        [FromForm] bool ParticipaComunionAncianos,
        [FromForm] bool TieneAlergiaAlimentaria,
        [FromForm] string? DescripcionAlergia,
        [FromForm] string? NecesidadesEspeciales,
        [FromForm(Name = "Servicios")] List<ServicioInscripcion>? Servicios,
        [FromForm] PreguntasAdicionalesNino? PreguntasAdicionalesNino)
    {
        var inscripcion = store.Inscripciones
            .Include(i => i.Persona)
            .FirstOrDefault(i => i.Id == id && i.EventoId == evento.Id);

        if (inscripcion is null) return NotFound();

        var cambios = new List<string>();
        var persona = inscripcion.Persona;
        var usuario = User.FindFirstValue(ClaimTypes.Name) ?? "Desconocido";

        // Track persona changes
        if (persona.Nombres != Nombres) { cambios.Add($"Nombres: {persona.Nombres} → {Nombres}"); persona.Nombres = Nombres; }
        if (persona.Apellidos != Apellidos) { cambios.Add($"Apellidos: {persona.Apellidos} → {Apellidos}"); persona.Apellidos = Apellidos; }
        if (persona.Genero != Genero) { cambios.Add($"Género: {persona.Genero.Humanize()} → {Genero.Humanize()}"); persona.Genero = Genero; }
        if (persona.FechaNacimiento != FechaNacimiento) { cambios.Add($"Fecha nacimiento: {persona.FechaNacimiento:dd/MM/yyyy} → {FechaNacimiento:dd/MM/yyyy}"); persona.FechaNacimiento = FechaNacimiento; }
        if (persona.Telefono != Telefono) { cambios.Add($"Teléfono: {persona.Telefono} → {Telefono}"); persona.Telefono = Telefono; }
        if (persona.Email != Email) { cambios.Add($"Email: {persona.Email ?? "—"} → {Email ?? "—"}"); persona.Email = Email; }

        // Track inscription changes
        if (inscripcion.Departamento != Departamento) { cambios.Add($"Departamento: {inscripcion.Departamento} → {Departamento}"); inscripcion.Departamento = Departamento; }
        if (inscripcion.Ciudad != Ciudad) { cambios.Add($"Ciudad: {inscripcion.Ciudad} → {Ciudad}"); inscripcion.Ciudad = Ciudad; }
        if (inscripcion.RequiereHospedaje != RequiereHospedaje) { cambios.Add($"Hospedaje: {(inscripcion.RequiereHospedaje ? "Sí" : "No")} → {(RequiereHospedaje ? "Sí" : "No")}"); inscripcion.RequiereHospedaje = RequiereHospedaje; }
        if (inscripcion.RequiereAlimentacion != RequiereAlimentacion) { cambios.Add($"Alimentación: {(inscripcion.RequiereAlimentacion ? "Sí" : "No")} → {(RequiereAlimentacion ? "Sí" : "No")}"); inscripcion.RequiereAlimentacion = RequiereAlimentacion; }
        if (inscripcion.ParticipaComunionAncianos != ParticipaComunionAncianos) { cambios.Add($"Comunión: {(inscripcion.ParticipaComunionAncianos ? "Sí" : "No")} → {(ParticipaComunionAncianos ? "Sí" : "No")}"); inscripcion.ParticipaComunionAncianos = ParticipaComunionAncianos; }
        if (inscripcion.TieneAlergiaAlimentaria != TieneAlergiaAlimentaria) { cambios.Add($"Alergia: {(inscripcion.TieneAlergiaAlimentaria ? "Sí" : "No")} → {(TieneAlergiaAlimentaria ? "Sí" : "No")}"); inscripcion.TieneAlergiaAlimentaria = TieneAlergiaAlimentaria; }
        if (inscripcion.DescripcionAlergia != DescripcionAlergia) { inscripcion.DescripcionAlergia = DescripcionAlergia; }
        if (inscripcion.NecesidadesEspeciales != NecesidadesEspeciales) { inscripcion.NecesidadesEspeciales = NecesidadesEspeciales; }

        inscripcion.Servicios = Servicios ?? [];
        inscripcion.PreguntasAdicionalesNino = PreguntasAdicionalesNino;
        inscripcion.FechaActualizacion = DateTime.UtcNow;

        if (cambios.Count > 0)
        {
            store.AuditoriaAdmin.Add(new AuditoriaAdmin
            {
                InscripcionId = id,
                Usuario = usuario,
                Accion = "Edición de inscripción",
                ValorAnterior = string.Join("; ", cambios.Select(c => c.Split(" → ")[0])),
                ValorNuevo = string.Join("; ", cambios.Select(c => c.Contains(" → ") ? c.Split(" → ")[1] : c))
            });
        }

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
                return View("~/Features/Admin/Inscripciones/Views/SinAcceso.cshtml");
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
            "Tipo Identificación", "Número Identificación", "Edad",
            "Teléfono", "Email", "Departamento", "Ciudad", "Estado", "Hospedaje",
            "Grupo Familiar", "Servicios", "Necesidades Especiales",
            "Alergia Alimentaria", "Descripción Alergia",
            "Comunión Ancianos/Diácono/Diaconisa", "Servicio Alimentación",
            "Participa FV KIDS", "Tipo de Sangre", "EPS",
            "Fecha Registro" };

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
            ws.Cell(row, 7).Value = ins.Persona.Edad;
            ws.Cell(row, 8).Value = ins.Persona.Telefono;
            ws.Cell(row, 9).Value = ins.Persona.Email ?? "";
            ws.Cell(row, 10).Value = ins.Departamento;
            ws.Cell(row, 11).Value = ins.Ciudad;
            ws.Cell(row, 12).Value = ins.Estado.Humanize();
            var estadoColor = ins.Estado switch
            {
                EstadoInscripcion.Completado => "#27ae60",
                EstadoInscripcion.Abono2 => "#6abf4b",
                EstadoInscripcion.Abono1 => "#a3d977",
                EstadoInscripcion.Pendiente => "#f39c12",
                EstadoInscripcion.NoVaAsistir => "#e74c3c",
                _ => "#999999"
            };
            ws.Cell(row, 12).Style.Font.FontColor = XLColor.White;
            ws.Cell(row, 12).Style.Fill.BackgroundColor = XLColor.FromHtml(estadoColor);
            ws.Cell(row, 13).Value = ins.RequiereHospedaje ? "Sí" : "No";
            ws.Cell(row, 14).Value = ins.GrupoFamiliar?.Id.ToString() ?? "";
            ws.Cell(row, 15).Value = string.Join(", ", ins.Servicios.Select(s => s.Descripcion()));
            ws.Cell(row, 16).Value = ins.NecesidadesEspeciales ?? "";
            ws.Cell(row, 17).Value = ins.TieneAlergiaAlimentaria ? "Sí" : "No";
            ws.Cell(row, 18).Value = ins.DescripcionAlergia ?? "";
            ws.Cell(row, 19).Value = ins.ParticipaComunionAncianos ? "Sí" : "No";
            ws.Cell(row, 20).Value = ins.RequiereAlimentacion ? "Sí" : "No";
            ws.Cell(row, 21).Value = ins.PreguntasAdicionalesNino is not null ? (ins.PreguntasAdicionalesNino.ParticipaFvKids ? "Sí" : "No") : "";
            ws.Cell(row, 22).Value = ins.PreguntasAdicionalesNino?.TipoSangre?.Descripcion() ?? "";
            ws.Cell(row, 23).Value = ins.PreguntasAdicionalesNino?.Eps ?? "";
            ws.Cell(row, 24).Value = ins.FechaCreacion.ToString("dd/MM/yyyy HH:mm");
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
