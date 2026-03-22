using AgendaPositiva.Web.Datos;
using AgendaPositiva.Web.Features.Commons;
using AgendaPositiva.Web.Features.Inscripciones.Dominio;
using AgendaPositiva.Web.Features.Inscripciones.Operaciones;
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
            return RedirectToAction("Formulario", new { numeroIdentificacion = datos.NumeroIdentificacion, tipoIdentificacion = datos.TipoIdentificacion });
        }
        else
        {
            return RedirectToAction("Detalles", new { id = inscripcion.Id });
        }
    }

    [HttpGet("detalles/{id}")]
    public IActionResult Detalles(int id)
    {
        Inscripcion? inscripcion = store.Inscripciones
            .Include(i => i.Persona)
            .Include(i => i.GrupoAsistencia)
                .ThenInclude(g => g!.LiderGrupo)
            .Include(i => i.GrupoAsistencia)
                .ThenInclude(g => g!.Inscripciones)
                    .ThenInclude(i => i.Persona)
            .FirstOrDefault(i => i.Id == id);

        if (inscripcion == null)
        {
            return NotFound();
        }

        return View(inscripcion);
    }

    [HttpGet("formulario")]
    public IActionResult Formulario(string numeroIdentificacion, TipoIdentificacion tipoIdentificacion)
    {
        return View(new { NumeroIdentificacion = numeroIdentificacion, TipoIdentificacion = tipoIdentificacion });
    }

    [HttpPost("formulario")]
    public async Task<IActionResult> PostFormulario([FromForm] RegistrarPreInscricionCommand request, [FromServices] RegistrarPreInscricion handler)
    {
        var result = await handler.Handle(request);

        if (result.IsSuccess)
        {
            return RedirectToAction("Detalles", new { id = result.Value });
        }
        else
        {
            return View("Formulario", request);
        }
    }


}