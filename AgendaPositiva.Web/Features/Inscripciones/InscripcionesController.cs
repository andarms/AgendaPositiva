using AgendaPositiva.Web.Datos;
using AgendaPositiva.Web.Features.Commons;
using AgendaPositiva.Web.Features.Inscripciones.Dominio;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgendaPositiva.Web.Features.Inscripciones;

public record VerificacionRequest(string NumeroIdentificacion, TipoIdentificacion TipoIdentificacion)
{
    public VerificacionRequest Sanitize()
    {
        // remueve espacio puntos y guiones para estandarizar el número de identificación
        string numero = NumeroIdentificacion.Replace(" ", "").Replace("-", "").Replace(".", "");
        return new VerificacionRequest(
            NumeroIdentificacion: numero,
            TipoIdentificacion: TipoIdentificacion
        );
    }
}

[Route("inscripciones")]
public class InscripcionesController : Controller
{
    readonly AppDbContext store;
    readonly Evento evento;

    public InscripcionesController(AppDbContext db)
    {
        store = db;
        evento = store.Eventos.FirstOrDefault(e => e.Activo) ?? throw new Exception("No hay un evento activo");
    }

    [HttpGet("verificacion")]
    public IActionResult Verificacion()
    {
        return View(new { Evento = evento });
    }

    [HttpPost("verificacion")]
    public IActionResult VerificarDocumento([FromForm] VerificacionRequest request)
    {
        VerificacionRequest datos = request.Sanitize();

        Inscripcion? inscripcion = store.Inscripciones
            .Include(i => i.Persona)
            .FirstOrDefault(i =>
                i.Persona.NumeroIdentificacion == datos.NumeroIdentificacion &&
                i.Persona.TipoIdentificacion == datos.TipoIdentificacion
            );

        if (inscripcion == null)
        {
            return RedirectToAction("Formulario");
        }
        else
        {
            return RedirectToAction("Detalles", new { id = inscripcion.Id });
        }
    }

    [HttpGet("detalles/{id}")]
    public IActionResult Detalles(int id)
    {
        Inscripcion? inscripcion = store.Inscripciones.FirstOrDefault(i => i.Id == id);

        if (inscripcion == null)
        {
            return NotFound();
        }

        return View(inscripcion);
    }

    [HttpGet("formulario")]
    public IActionResult Formulario()
    {
        return View();
    }
}