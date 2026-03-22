using Microsoft.AspNetCore.Mvc;

namespace AgendaPositiva.Web.Features.Landing;

public class LandingController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}