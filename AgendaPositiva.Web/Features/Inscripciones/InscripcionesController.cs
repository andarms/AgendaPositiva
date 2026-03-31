using AgendaPositiva.Web.Datos;
using AgendaPositiva.Web.Features.Commons;
using AgendaPositiva.Web.Features.Inscripciones.Dominio;
using AgendaPositiva.Web.Features.Inscripciones.Operaciones;
using AgendaPositiva.Web.Features.Inscripciones.Views;
using AgendaPositiva.Web.Features.Inscripciones.Views.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgendaPositiva.Web.Features.Inscripciones;

public record VerificacionDto(string NumeroIdentificacion, TipoIdentificacion TipoIdentificacion)
{
    public VerificacionDto Sanitize()
    {
        // remueve espacio puntos y guiones para estandarizar el número de identificación
        string numero = NumeroIdentificacion.Replace(" ", "").Replace("-", "").Replace(".", "");
        return new VerificacionDto(
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

    // ==========================================
    // Verificación inicial
    // ==========================================

    public IActionResult Index()
    {
        return RedirectToAction("Verificacion");
    }

    [HttpGet("verificacion")]
    public IActionResult Verificacion()
    {
        return View(new { Evento = evento });
    }

    [HttpPost("verificacion")]
    public IActionResult VerificarDocumento([FromForm] VerificacionDto request)
    {
        VerificacionDto datos = request.Sanitize();

        Inscripcion? inscripcion = store.Inscripciones
            .Include(i => i.Persona)
            .FirstOrDefault(i =>
                i.Persona.NumeroIdentificacion == datos.NumeroIdentificacion &&
                i.Persona.TipoIdentificacion == datos.TipoIdentificacion
            );

        if (inscripcion == null)
        {
            return RedirectToAction("FormularioPersonal", new { numeroIdentificacion = datos.NumeroIdentificacion, tipoIdentificacion = datos.TipoIdentificacion });
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
            .Include(i => i.RelacionConPersona)
            .Include(i => i.GrupoFamiliar)
                .ThenInclude(g => g!.Inscripciones)
                    .ThenInclude(i => i.Persona)
            .Include(i => i.GrupoFamiliar)
                .ThenInclude(g => g!.Inscripciones)
                    .ThenInclude(i => i.RelacionConPersona)
            .FirstOrDefault(i => i.Id == id);

        if (inscripcion == null)
        {
            return NotFound();
        }

        return View(inscripcion);
    }

    [HttpGet("buscar-persona")]
    public IActionResult BuscarPersona([FromQuery] string numeroIdentificacion, [FromQuery] TipoIdentificacion tipoIdentificacion)
    {
        var datos = new VerificacionDto(numeroIdentificacion, tipoIdentificacion).Sanitize();

        var persona = store.Personas
            .FirstOrDefault(p =>
                p.NumeroIdentificacion == datos.NumeroIdentificacion &&
                p.TipoIdentificacion == datos.TipoIdentificacion);

        if (persona is null)
            return NotFound();

        return Json(new
        {
            persona.Id,
            persona.Nombres,
            persona.Apellidos,
            Genero = persona.Genero.ToString(),
            FechaNacimiento = persona.FechaNacimiento.ToString("yyyy-MM-dd"),
            persona.Telefono,
            persona.Email,
            TipoIdentificacion = persona.TipoIdentificacion.ToString(),
            persona.NumeroIdentificacion
        });
    }

    // ==========================================
    // PASO 1: Formulario Personal
    // ==========================================

    [HttpGet("formulario-personal")]
    public IActionResult FormularioPersonal(string numeroIdentificacion, TipoIdentificacion tipoIdentificacion)
    {
        return View(new { NumeroIdentificacion = numeroIdentificacion, TipoIdentificacion = tipoIdentificacion });
    }

    [HttpPost("formulario-personal")]
    public async Task<IActionResult> PostFormularioPersonal(
        [FromForm] RegistrarPersonaPrincipalDto command,
        [FromForm(Name = "Servicios")] List<ServicioInscripcion>? servicios,
        [FromForm] string? accion,
        [FromServices] RegistrarPersonaPrincipal handler)
    {
        var result = await handler.Handle(command with { Servicios = servicios });

        if (result.IsSuccess)
        {
            if (accion == "terminar")
                return RedirectToAction("Detalles", new { id = result.Value });

            return RedirectToAction("VerificarFamiliares", new { inscripcionId = result.Value });
        }

        return View("FormularioPersonal", command);
    }

    // ==========================================
    // PASO 2: Verificar Familiares
    // ==========================================

    [HttpGet("verificar-familiares/{inscripcionId}")]
    public IActionResult VerificarFamiliares(int inscripcionId)
    {
        var inscripcion = store.Inscripciones
            .Include(i => i.GrupoFamiliar)
                .ThenInclude(g => g!.Inscripciones)
                    .ThenInclude(i => i.Persona)
            .FirstOrDefault(i => i.Id == inscripcionId);

        if (inscripcion is null) return NotFound();

        var familiares = inscripcion.GrupoFamiliarId is not null
            ? inscripcion.GrupoFamiliar!.Inscripciones
                .Where(i => i.Id != inscripcionId).ToList()
            : [];

        return View(new VerificarFamiliaresViewModel
        {
            InscripcionId = inscripcionId,
            FamiliaresAgregados = familiares
        });
    }

    [HttpPost("verificar-familiares/{inscripcionId}")]
    public IActionResult BuscarFamiliarEnGrupo(int inscripcionId, [FromForm] VerificacionDto request)
    {
        var datos = request.Sanitize();

        var persona = store.Personas
            .FirstOrDefault(p =>
                p.NumeroIdentificacion == datos.NumeroIdentificacion &&
                p.TipoIdentificacion == datos.TipoIdentificacion);

        if (persona is not null)
        {
            var inscripcion = store.Inscripciones
                .Include(i => i.GrupoFamiliar)
                    .ThenInclude(g => g!.Inscripciones)
                        .ThenInclude(i => i.Persona)
                .FirstOrDefault(i => i.Id == inscripcionId);

            var familiares = inscripcion?.GrupoFamiliarId is not null
                ? [.. inscripcion.GrupoFamiliar!.Inscripciones.Where(i => i.Id != inscripcionId)]
                : new List<Inscripcion>();

            return View("VerificarFamiliares", new VerificarFamiliaresViewModel
            {
                InscripcionId = inscripcionId,
                PersonaEncontrada = persona,
                FamiliaresAgregados = familiares
            });
        }

        return RedirectToAction("FormularioFamiliar", new
        {
            inscripcionId,
            numeroIdentificacion = datos.NumeroIdentificacion,
            tipoIdentificacion = datos.TipoIdentificacion
        });
    }

    [HttpPost("agregar-familiar/{inscripcionId}")]
    public async Task<IActionResult> PostAgregarFamiliarExistente(
        int inscripcionId,
        [FromForm] int personaExistenteId,
        [FromForm] Parentesco parentesco,
        [FromServices] AgregarAlGrupo handler)
    {
        var inscripcion = store.Inscripciones
            .Include(i => i.Persona)
            .FirstOrDefault(i => i.Id == inscripcionId);
        if (inscripcion is null) return NotFound();

        // Si elijo "A es mi Tío", C guarda "Sobrino de A"
        var inverso = parentesco.ObtenerInverso(inscripcion.Persona.Genero);
        await handler.ConPersonaExistente(inscripcionId, personaExistenteId, inverso, personaExistenteId);
        return RedirectToAction("VerificarFamiliares", new { inscripcionId });
    }

    [HttpGet("formulario-familiar/{inscripcionId}")]
    public IActionResult FormularioFamiliar(int inscripcionId, string? numeroIdentificacion, TipoIdentificacion? tipoIdentificacion)
    {
        var inscripcion = store.Inscripciones.FirstOrDefault(i => i.Id == inscripcionId);
        return View(new
        {
            InscripcionId = inscripcionId,
            NumeroIdentificacion = numeroIdentificacion ?? "",
            TipoIdentificacion = tipoIdentificacion?.ToString() ?? "",
            DepartamentoDefault = inscripcion?.Departamento ?? "",
            CiudadDefault = inscripcion?.Ciudad ?? ""
        });
    }

    [HttpPost("formulario-familiar/{inscripcionId}")]
    public async Task<IActionResult> PostFormularioFamiliar(
        int inscripcionId,
        [FromForm] FamiliarDto datos,
        [FromForm(Name = "Servicios")] List<ServicioInscripcion>? servicios,
        [FromServices] AgregarAlGrupo handler)
    {
        var inscripcion = store.Inscripciones.FirstOrDefault(i => i.Id == inscripcionId);
        if (inscripcion is null) return NotFound();

        var relacion = datos.Parentesco ?? Parentesco.Otro;
        var datosConServicios = datos with { Servicios = servicios };

        var result = await handler.ConPersonaNueva(inscripcionId, datosConServicios, relacion, inscripcion.PersonaId);

        if (result.IsSuccess)
            return RedirectToAction("VerificarFamiliares", new { inscripcionId });

        return View("FormularioFamiliar", new
        {
            InscripcionId = inscripcionId,
            NumeroIdentificacion = datos.NumeroIdentificacion,
            TipoIdentificacion = datos.TipoIdentificacion.ToString(),
            DepartamentoDefault = datos.Departamento,
            CiudadDefault = datos.Ciudad
        });
    }
}