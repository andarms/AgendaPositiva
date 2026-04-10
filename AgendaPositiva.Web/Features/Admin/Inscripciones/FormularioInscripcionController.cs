using AgendaPositiva.Web.Datos;
using AgendaPositiva.Web.Features.Commons;
using AgendaPositiva.Web.Features.Inscripciones.Dominio;
using AgendaPositiva.Web.Features.Inscripciones.Operaciones;
using AgendaPositiva.Web.Features.Admin.Inscripciones.Views.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AgendaPositiva.Web.Features.Admin.Inscripciones;

public record VerificacionDto(string NumeroIdentificacion, TipoIdentificacion TipoIdentificacion)
{
    public VerificacionDto Sanitize()
    {
        // remueve todo lo que no sea dígito
        string numero = Regex.Replace(NumeroIdentificacion, @"[^0-9]", "");

        if (string.IsNullOrEmpty(numero))
            throw new ArgumentException("El número de identificación debe contener solo números.");

        return new VerificacionDto(
            NumeroIdentificacion: numero,
            TipoIdentificacion: TipoIdentificacion
        );
    }
}

[Route("admin/inscripciones/formulario")]
[Authorize(Policy = "AdminPanel")]
public class FormularioInscripcionController : Controller
{
    readonly AppDbContext store;
    readonly Evento evento;
    readonly UbicacionService ubicacionService;

    public FormularioInscripcionController(AppDbContext db, UbicacionService ubicacionService)
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

    List<string> DepartamentosDisponibles => EsAdministrador
        ? ubicacionService.ObtenerNombresDepartamentos()
        : [.. LocalidadesAsignadas.Keys];

    bool TieneAcceso(string departamento, string ciudad)
    {
        if (EsAdministrador) return true;
        var localidades = LocalidadesAsignadas;
        if (!localidades.TryGetValue(departamento, out var ciudades)) return false;
        return ciudades.Count == 0 || ciudades.Contains(ciudad);
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
        return View("~/Features/Admin/Inscripciones/Views/Formulario/Verificacion.cshtml", new { Evento = evento });
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

        if (!TieneAcceso(inscripcion.Departamento, inscripcion.Ciudad))
        {
            return RedirectToAction("SinAcceso");
        }

        return Redirect($"/admin/inscripciones/{inscripcion.Id}");
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
        ViewBag.NombreEvento = evento.Nombre;
        ViewBag.DepartamentosDisponibles = DepartamentosDisponibles;
        return View("~/Features/Admin/Inscripciones/Views/Formulario/FormularioPersonal.cshtml", new { NumeroIdentificacion = numeroIdentificacion, TipoIdentificacion = tipoIdentificacion });
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
                return Redirect($"/admin/inscripciones/{result.Value}");

            return RedirectToAction("VerificarFamiliares", new { inscripcionId = result.Value });
        }

        ViewBag.Error = result.Error;
        ViewBag.DepartamentosDisponibles = DepartamentosDisponibles;
        return View("~/Features/Admin/Inscripciones/Views/Formulario/FormularioPersonal.cshtml", command);
    }

    // ==========================================
    // PASO 2: Verificar Familiares
    // ==========================================

    [HttpGet("verificar-familiares/{inscripcionId}")]
    public IActionResult VerificarFamiliares(int inscripcionId)
    {
        var inscripcion = store.Inscripciones
            .Include(i => i.Persona)
            .Include(i => i.GrupoFamiliar)
                .ThenInclude(g => g!.Inscripciones)
                    .ThenInclude(i => i.Persona)
            .FirstOrDefault(i => i.Id == inscripcionId);

        if (inscripcion is null) return NotFound();

        var familiares = inscripcion.GrupoFamiliarId is not null
            ? inscripcion.GrupoFamiliar!.Inscripciones
                .Where(i => i.Id != inscripcionId).ToList()
            : [];

        return View("~/Features/Admin/Inscripciones/Views/Formulario/VerificarFamiliares.cshtml", new VerificarFamiliaresViewModel
        {
            InscripcionId = inscripcionId,
            NombrePersona = inscripcion.Persona.NombreCompleto,
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
                .Include(i => i.Persona)
                .Include(i => i.GrupoFamiliar)
                    .ThenInclude(g => g!.Inscripciones)
                        .ThenInclude(i => i.Persona)
                .FirstOrDefault(i => i.Id == inscripcionId);

            var familiares = inscripcion?.GrupoFamiliarId is not null
                ? [.. inscripcion.GrupoFamiliar!.Inscripciones.Where(i => i.Id != inscripcionId)]
                : new List<Inscripcion>();

            return View("~/Features/Admin/Inscripciones/Views/Formulario/VerificarFamiliares.cshtml", new VerificarFamiliaresViewModel
            {
                InscripcionId = inscripcionId,
                NombrePersona = inscripcion?.Persona.NombreCompleto ?? "",
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
        ViewBag.DepartamentosDisponibles = DepartamentosDisponibles;
        return View("~/Features/Admin/Inscripciones/Views/Formulario/FormularioFamiliar.cshtml", new
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

        ViewBag.DepartamentosDisponibles = DepartamentosDisponibles;
        return View("~/Features/Admin/Inscripciones/Views/Formulario/FormularioFamiliar.cshtml", new
        {
            InscripcionId = inscripcionId,
            NumeroIdentificacion = datos.NumeroIdentificacion,
            TipoIdentificacion = datos.TipoIdentificacion.ToString(),
            DepartamentoDefault = datos.Departamento,
            CiudadDefault = datos.Ciudad
        });
    }

    // ==========================================
    // Sin acceso
    // ==========================================

    [HttpGet("sin-acceso")]
    public IActionResult SinAcceso()
    {
        return View("~/Features/Admin/Inscripciones/Views/Formulario/SinAcceso.cshtml");
    }
}