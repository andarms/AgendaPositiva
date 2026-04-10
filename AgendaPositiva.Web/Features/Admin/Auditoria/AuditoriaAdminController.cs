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
        [FromQuery] string? accion,
        [FromQuery] int pagina = 1,
        [FromQuery] int porPagina = 50)
    {
        if (!User.IsInRole("Administrador"))
            return Forbid();

        porPagina = porPagina is 10 or 50 or 100 ? porPagina : 50;

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

        var orderedQuery = query.OrderByDescending(a => a.FechaCreacion);
        var total = orderedQuery.Count();
        var totalPaginas = (int)Math.Ceiling(total / (double)porPagina);
        if (pagina < 1) pagina = 1;
        if (pagina > totalPaginas && totalPaginas > 0) pagina = totalPaginas;

        var registros = orderedQuery
            .Skip((pagina - 1) * porPagina)
            .Take(porPagina)
            .ToList();

        ViewBag.FiltroUsuario = usuario;
        ViewBag.FiltroAccion = accion;
        ViewBag.Pagina = pagina;
        ViewBag.PorPagina = porPagina;
        ViewBag.TotalPaginas = totalPaginas;
        ViewBag.TotalRegistros = total;
        ViewBag.AccionesDisponibles = store.AuditoriaAdmin
            .Select(a => a.Accion)
            .Distinct()
            .OrderBy(a => a)
            .ToList();

        return View("~/Features/Admin/Auditoria/Views/Index.cshtml", registros);
    }
}
