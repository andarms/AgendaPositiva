using AgendaPositiva.Web.Datos;
using AgendaPositiva.Web.Features.Admin.Auth.Domain;
using AgendaPositiva.Web.Features.Inscripciones.Dominio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgendaPositiva.Web.Features.Admin.Usuarios;

[Route("admin/usuarios")]
[Authorize(Roles = "Administrador")]
public class UsuariosAdminController : Controller
{
    readonly AppDbContext store;
    readonly UbicacionService ubicacionService;

    public UsuariosAdminController(AppDbContext db, UbicacionService ubicacionService)
    {
        store = db;
        this.ubicacionService = ubicacionService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var usuarios = await store.UsuariosAdministradores
            .OrderBy(u => u.Nombre)
            .ToListAsync();

        return View("~/Features/Admin/Usuarios/Views/Index.cshtml", usuarios);
    }

    [HttpGet("crear")]
    public IActionResult Crear()
    {
        var vm = new UsuarioFormViewModel
        {
            DepartamentosDisponibles = ubicacionService.ObtenerNombresDepartamentos()
        };
        return View("~/Features/Admin/Usuarios/Views/Formulario.cshtml", vm);
    }

    [HttpPost("crear")]
    public async Task<IActionResult> CrearPost(UsuarioFormViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm.DepartamentosDisponibles = ubicacionService.ObtenerNombresDepartamentos();
            return View("~/Features/Admin/Usuarios/Views/Formulario.cshtml", vm);
        }

        var existe = await store.UsuariosAdministradores.AnyAsync(u => u.Email == vm.Email);
        if (existe)
        {
            ModelState.AddModelError("Email", "Ya existe un usuario con este correo.");
            vm.DepartamentosDisponibles = ubicacionService.ObtenerNombresDepartamentos();
            return View("~/Features/Admin/Usuarios/Views/Formulario.cshtml", vm);
        }

        var usuario = new UsuarioAdministrador
        {
            Email = vm.Email,
            Nombre = vm.Nombre,
            Rol = vm.Rol,
            Departamentos = vm.DepartamentosSeleccionados ?? [],
            Activo = vm.Activo
        };

        store.UsuariosAdministradores.Add(usuario);
        await store.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("editar/{id:int}")]
    public async Task<IActionResult> Editar(int id)
    {
        var usuario = await store.UsuariosAdministradores.FindAsync(id);
        if (usuario is null) return NotFound();

        var vm = new UsuarioFormViewModel
        {
            Id = usuario.Id,
            Email = usuario.Email,
            Nombre = usuario.Nombre ?? "",
            Rol = usuario.Rol,
            Activo = usuario.Activo,
            DepartamentosSeleccionados = usuario.Departamentos,
            DepartamentosDisponibles = ubicacionService.ObtenerNombresDepartamentos()
        };

        return View("~/Features/Admin/Usuarios/Views/Formulario.cshtml", vm);
    }

    [HttpPost("editar/{id:int}")]
    public async Task<IActionResult> EditarPost(int id, UsuarioFormViewModel vm)
    {
        var usuario = await store.UsuariosAdministradores.FindAsync(id);
        if (usuario is null) return NotFound();

        if (!ModelState.IsValid)
        {
            vm.Id = id;
            vm.DepartamentosDisponibles = ubicacionService.ObtenerNombresDepartamentos();
            return View("~/Features/Admin/Usuarios/Views/Formulario.cshtml", vm);
        }

        var emailDuplicado = await store.UsuariosAdministradores
            .AnyAsync(u => u.Email == vm.Email && u.Id != id);
        if (emailDuplicado)
        {
            ModelState.AddModelError("Email", "Ya existe otro usuario con este correo.");
            vm.Id = id;
            vm.DepartamentosDisponibles = ubicacionService.ObtenerNombresDepartamentos();
            return View("~/Features/Admin/Usuarios/Views/Formulario.cshtml", vm);
        }

        usuario.Email = vm.Email;
        usuario.Nombre = vm.Nombre;
        usuario.Rol = vm.Rol;
        usuario.Activo = vm.Activo;
        usuario.Departamentos = vm.DepartamentosSeleccionados ?? [];

        await store.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("eliminar/{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var usuario = await store.UsuariosAdministradores.FindAsync(id);
        if (usuario is null) return NotFound();

        store.UsuariosAdministradores.Remove(usuario);
        await store.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}

public class UsuarioFormViewModel
{
    public int? Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public RolAdministrador Rol { get; set; } = RolAdministrador.Colaborador;
    public bool Activo { get; set; } = true;
    public List<string>? DepartamentosSeleccionados { get; set; } = [];
    public List<string> DepartamentosDisponibles { get; set; } = [];
}
