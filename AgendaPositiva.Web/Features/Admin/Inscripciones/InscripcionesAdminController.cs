using AgendaPositiva.Web.Datos;
using AgendaPositiva.Web.Features.Commons;
using AgendaPositiva.Web.Features.Commons.Views;
using AgendaPositiva.Web.Features.Admin.Inscripciones.Views.ViewModels;
using AgendaPositiva.Web.Features.Inscripciones.Dominio;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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

    List<string> DepartamentosAsignados =>
        User.FindFirstValue("Departamentos")?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() ?? [];

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

        // Colaboradores solo ven sus departamentos asignados
        if (!EsAdministrador)
        {
            var deptos = DepartamentosAsignados;
            query = query.Where(i => deptos.Contains(i.Departamento));
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

    [HttpGet("exportar")]
    public IActionResult Exportar()
    {
        var query = store.Inscripciones
            .Include(i => i.Persona)
            .Include(i => i.GrupoFamiliar)
            .Where(i => i.EventoId == evento.Id);

        if (!EsAdministrador)
        {
            var deptos = DepartamentosAsignados;
            query = query.Where(i => deptos.Contains(i.Departamento));
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
            "Parentesco", "Necesidades Especiales", "Fecha Registro" };

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
            ws.Cell(row, 14).Value = relacion;
            ws.Cell(row, 15).Value = ins.NecesidadesEspeciales ?? "";
            ws.Cell(row, 16).Value = ins.FechaCreacion.ToString("dd/MM/yyyy HH:mm");
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
