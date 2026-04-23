using AgendaPositiva.Web.Datos;
using AgendaPositiva.Web.Features.Inscripciones.Dominio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgendaPositiva.Web.Features.Admin.Categorias;

[Route("admin/categorias")]
[Authorize(Roles = "Administrador")]
public class CategoriasAdminController : Controller
{
    readonly AppDbContext store;

    public CategoriasAdminController(AppDbContext db) => store = db;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var categorias = await store.CategoriasInscripcion
            .OrderBy(c => c.Nombre)
            .ToListAsync();

        return View("~/Features/Admin/Categorias/Views/Index.cshtml", categorias);
    }

    [HttpGet("crear")]
    public IActionResult Crear() =>
        View("~/Features/Admin/Categorias/Views/Formulario.cshtml", new CategoriaInscripcion());

    [HttpPost("crear")]
    public async Task<IActionResult> CrearPost(string nombre, decimal precio)
    {
        if (string.IsNullOrWhiteSpace(nombre) || precio < 0)
        {
            ModelState.AddModelError("", "Nombre y precio son requeridos.");
            return View("~/Features/Admin/Categorias/Views/Formulario.cshtml",
                new CategoriaInscripcion { Nombre = nombre ?? "", Precio = precio });
        }

        store.CategoriasInscripcion.Add(new CategoriaInscripcion { Nombre = nombre.Trim(), Precio = precio });
        await store.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("editar/{id:int}")]
    public async Task<IActionResult> Editar(int id)
    {
        var cat = await store.CategoriasInscripcion.FindAsync(id);
        if (cat is null) return NotFound();
        return View("~/Features/Admin/Categorias/Views/Formulario.cshtml", cat);
    }

    [HttpPost("editar/{id:int}")]
    public async Task<IActionResult> EditarPost(int id, string nombre, decimal precio)
    {
        var cat = await store.CategoriasInscripcion.FindAsync(id);
        if (cat is null) return NotFound();

        if (string.IsNullOrWhiteSpace(nombre) || precio < 0)
        {
            ModelState.AddModelError("", "Nombre y precio son requeridos.");
            return View("~/Features/Admin/Categorias/Views/Formulario.cshtml", cat);
        }

        cat.Nombre = nombre.Trim();
        cat.Precio = precio;
        await store.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/toggle")]
    public async Task<IActionResult> Toggle(int id)
    {
        var cat = await store.CategoriasInscripcion.FindAsync(id);
        if (cat is null) return NotFound();

        cat.Activa = !cat.Activa;
        await store.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
