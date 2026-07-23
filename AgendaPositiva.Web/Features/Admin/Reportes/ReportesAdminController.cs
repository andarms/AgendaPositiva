using AgendaPositiva.Web.Datos;
using AgendaPositiva.Web.Features.Commons;
using AgendaPositiva.Web.Features.Inscripciones.Dominio;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgendaPositiva.Web.Features.Admin.Reportes;

[Route("admin/reportes")]
[Authorize(Roles = "Administrador")]
public class ReportesAdminController : Controller
{
    readonly AppDbContext store;
    readonly Evento evento;

    public ReportesAdminController(AppDbContext db)
    {
        store = db;
        evento = store.Eventos.FirstOrDefault(e => e.Activo) ?? throw new Exception("No hay un evento activo");
    }

    [HttpGet("")]
    public IActionResult Index() => View("~/Features/Admin/Reportes/Views/Index.cshtml");

    [HttpGet("ninos")]
    public IActionResult ExportarNinos()
    {
        var fechaCorte = DateOnly.FromDateTime(DateTime.Today).AddYears(-11);

        var inscripciones = store.Inscripciones
            .Include(i => i.Persona)
            .Include(i => i.GrupoFamiliar).ThenInclude(g => g!.Inscripciones).ThenInclude(i => i.Persona)
            .Where(i => i.EventoId == evento.Id)
            .Where(i => i.Estado != EstadoInscripcion.NoVaAsistir)
            .Where(i => i.Persona.FechaNacimiento > fechaCorte)
            .OrderBy(i => i.Departamento)
            .ThenBy(i => i.Ciudad)
            .ToList();

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Niños");

        var headers = new[] { "#", "Nombre Completo", "Edad", "Departamento", "Ciudad",
            "Tipo de Sangre", "Es Alérgico", "Descripción Alergia", "Nombre Acudiente", "Teléfono Acudiente" };

        for (int i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];

        var headerRow = ws.Range(1, 1, 1, headers.Length);
        headerRow.Style.Font.Bold = true;
        headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#1a1a2e");
        headerRow.Style.Font.FontColor = XLColor.White;

        for (int i = 0; i < inscripciones.Count; i++)
        {
            var ins = inscripciones[i];
            int row = i + 2;

            ws.Cell(row, 1).Value = i + 1;
            ws.Cell(row, 2).Value = ins.Persona.NombreCompleto;
            ws.Cell(row, 3).Value = ins.Persona.Edad;
            ws.Cell(row, 4).Value = ins.Departamento;
            ws.Cell(row, 5).Value = ins.Ciudad;
            ws.Cell(row, 6).Value = ins.PreguntasAdicionalesNino?.TipoSangre?.Descripcion() ?? "";
            ws.Cell(row, 7).Value = ins.TieneAlergiaAlimentaria ? "Sí" : "No";
            ws.Cell(row, 8).Value = ins.DescripcionAlergia ?? "";
            var acudiente = ObtenerAcudiente(ins);
            ws.Cell(row, 9).Value = acudiente?.Persona.NombreCompleto ?? "Sin acudiente";
            ws.Cell(row, 10).Value = acudiente?.Persona.Telefono ?? "";
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var fileName = $"Ninos_{evento.Nombre.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}.xlsx";
        return File(stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    static Inscripcion? ObtenerAcudiente(Inscripcion inscripcionNino)
    {
        var miembros = inscripcionNino.GrupoFamiliar?.Inscripciones;
        if (miembros is null || miembros.Count <= 1)
            return null;

        var padreOMadre = miembros.FirstOrDefault(m =>
            m.Id != inscripcionNino.Id &&
            m.Relacion is Parentesco.Padre or Parentesco.Madre);

        if (padreOMadre is not null)
            return padreOMadre;

        // ponytail: fallback — primer adulto del grupo
        return miembros.FirstOrDefault(m =>
            m.Id != inscripcionNino.Id && m.Persona.EsMayorDeEdad);
    }
}
