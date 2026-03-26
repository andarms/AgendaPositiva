using Microsoft.AspNetCore.Mvc;

namespace AgendaPositiva.Web.Features.Landing;

public class LandingController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    [Route("no-encontrado")]
    public IActionResult NoEncontrado()
    {
        Response.StatusCode = 404;
        return View("~/Features/Landing/Views/NoEncontrado.cshtml");
    }
}