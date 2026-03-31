using AgendaPositiva.Web.Datos;
using AgendaPositiva.Web.Features.Admin.Auditoria;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgendaPositiva.Web.Features.Admin.Auditoria;

[Route("admin/auditoria")]
[Authorize(Policy = "AdminPanel")]
public class AuditoriaAdminController : Controller
{
    readonly AppDbContext store;

    public AuditoriaAdminController(AppDbContext db)
    {
        store = db;
    }

    [HttpGet]
    public IActionResult Index(
        [FromQuery] string? usuario,
        [FromQuery] string? accion)
    {
        if (!User.IsInRole("Administrador"))
            return Forbid();

        var query = store.AuditoriaAdmin.AsQueryable();

        if (!string.IsNullOrWhiteSpace(usuario))
        {
            var term = usuario.Trim().ToLower();
            query = query.Where(a => a.Usuario.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(accion))
        {
            query = query.Where(a => a.Accion == accion);
        }

        var registros = query
            .OrderByDescending(a => a.FechaCreacion)
            .Take(500)
            .ToList();

        ViewBag.FiltroUsuario = usuario;
        ViewBag.FiltroAccion = accion;
        ViewBag.AccionesDisponibles = store.AuditoriaAdmin
            .Select(a => a.Accion)
            .Distinct()
            .OrderBy(a => a)
            .ToList();

        return View("~/Features/Admin/Auditoria/Views/Index.cshtml", registros);
    }
}
