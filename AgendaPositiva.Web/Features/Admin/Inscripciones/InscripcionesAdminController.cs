using AgendaPositiva.Web.Datos;
using AgendaPositiva.Web.Features.Commons;
using AgendaPositiva.Web.Features.Admin.Inscripciones.Views.ViewModels;
using AgendaPositiva.Web.Features.Inscripciones.Dominio;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgendaPositiva.Web.Features.Admin.Inscripciones;

[Route("admin/inscripciones")]
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
            Departamentos = ubicacionService.ObtenerNombresDepartamentos(),
            FiltroNombre = nombre,
            FiltroDepartamento = departamento,
            FiltroEstado = estado,
            FiltroHospedaje = hospedaje,
            SortLocalidad = sortLocalidad
        };

        return View("~/Features/Admin/Inscripciones/Views/Index.cshtml", vm);
    }
}
