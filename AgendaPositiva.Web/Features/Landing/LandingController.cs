using AgendaPositiva.Web.Datos;
using Microsoft.AspNetCore.Mvc;

namespace AgendaPositiva.Web.Features.Landing;

public class LandingController(AppDbContext db) : Controller
{
    public IActionResult Index()
    {
        var evento = db.Eventos.FirstOrDefault(e => e.Activo);
        return View(evento);
    }

    [Route("no-encontrado")]
    public IActionResult NoEncontrado()
    {
        Response.StatusCode = 404;
        return View("~/Features/Landing/Views/NoEncontrado.cshtml");
    }

    [Route("politica-privacidad")]
    public IActionResult PoliticaPrivacidad()
    {
        return View("~/Features/Landing/Views/PoliticaPrivacidad.cshtml");
    }

    [Route("preguntas-frecuentes")]
    public IActionResult PreguntasFrecuentes()
    {
        return View("~/Features/Landing/Views/PreguntasFrecuentes.cshtml");
    }
}