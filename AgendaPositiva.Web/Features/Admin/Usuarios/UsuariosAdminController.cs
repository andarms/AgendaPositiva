using AgendaPositiva.Web.Datos;
using AgendaPositiva.Web.Features.Admin.Auth.Domain;
using AgendaPositiva.Web.Features.Inscripciones.Dominio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

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

    UsuarioFormViewModel CrearViewModel(UsuarioAdministrador? usuario = null)
    {
        var departamentosInfo = ubicacionService.Departamentos
            .Select(d => new DepartamentoFormItem
            {
                Nombre = d.Departamento,
                CiudadesDisponibles = d.Ciudades
            }).ToList();

        var vm = new UsuarioFormViewModel
        {
            DepartamentosInfo = departamentosInfo
        };

        if (usuario is not null)
        {
            vm.Id = usuario.Id;
            vm.Email = usuario.Email;
            vm.Nombre = usuario.Nombre ?? "";
            vm.Rol = usuario.Rol;
            vm.Activo = usuario.Activo;
            vm.LocalidadesJson = JsonSerializer.Serialize(usuario.Localidades);
        }

        return vm;
    }

    [HttpGet("crear")]
    public IActionResult Crear()
    {
        var vm = CrearViewModel();
        return View("~/Features/Admin/Usuarios/Views/Formulario.cshtml", vm);
    }

    [HttpPost("crear")]
    public async Task<IActionResult> CrearPost(UsuarioFormViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var vmErr = CrearViewModel();
            vmErr.Email = vm.Email;
            vmErr.Nombre = vm.Nombre;
            vmErr.Rol = vm.Rol;
            vmErr.Activo = vm.Activo;
            vmErr.LocalidadesJson = vm.LocalidadesJson;
            return View("~/Features/Admin/Usuarios/Views/Formulario.cshtml", vmErr);
        }

        var existe = await store.UsuariosAdministradores.AnyAsync(u => u.Email == vm.Email);
        if (existe)
        {
            ModelState.AddModelError("Email", "Ya existe un usuario con este correo.");
            var vmErr = CrearViewModel();
            vmErr.Email = vm.Email;
            vmErr.Nombre = vm.Nombre;
            vmErr.Rol = vm.Rol;
            vmErr.Activo = vm.Activo;
            vmErr.LocalidadesJson = vm.LocalidadesJson;
            return View("~/Features/Admin/Usuarios/Views/Formulario.cshtml", vmErr);
        }

        var usuario = new UsuarioAdministrador
        {
            Email = vm.Email,
            Nombre = vm.Nombre,
            Rol = vm.Rol,
            Localidades = vm.ParsearLocalidades(),
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

        var vm = CrearViewModel(usuario);
        return View("~/Features/Admin/Usuarios/Views/Formulario.cshtml", vm);
    }

    [HttpPost("editar/{id:int}")]
    public async Task<IActionResult> EditarPost(int id, UsuarioFormViewModel vm)
    {
        var usuario = await store.UsuariosAdministradores.FindAsync(id);
        if (usuario is null) return NotFound();

        if (!ModelState.IsValid)
        {
            var vmErr = CrearViewModel(usuario);
            vmErr.Email = vm.Email;
            vmErr.Nombre = vm.Nombre;
            vmErr.Rol = vm.Rol;
            vmErr.Activo = vm.Activo;
            vmErr.LocalidadesJson = vm.LocalidadesJson;
            return View("~/Features/Admin/Usuarios/Views/Formulario.cshtml", vmErr);
        }

        var emailDuplicado = await store.UsuariosAdministradores
            .AnyAsync(u => u.Email == vm.Email && u.Id != id);
        if (emailDuplicado)
        {
            ModelState.AddModelError("Email", "Ya existe otro usuario con este correo.");
            var vmErr = CrearViewModel(usuario);
            vmErr.Email = vm.Email;
            vmErr.Nombre = vm.Nombre;
            vmErr.Rol = vm.Rol;
            vmErr.Activo = vm.Activo;
            vmErr.LocalidadesJson = vm.LocalidadesJson;
            return View("~/Features/Admin/Usuarios/Views/Formulario.cshtml", vmErr);
        }

        usuario.Email = vm.Email;
        usuario.Nombre = vm.Nombre;
        usuario.Rol = vm.Rol;
        usuario.Activo = vm.Activo;
        usuario.Localidades = vm.ParsearLocalidades();

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

public class DepartamentoFormItem
{
    public string Nombre { get; set; } = string.Empty;
    public List<string> CiudadesDisponibles { get; set; } = [];
}

public class UsuarioFormViewModel
{
    public int? Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public RolAdministrador Rol { get; set; } = RolAdministrador.Colaborador;
    public bool Activo { get; set; } = true;

    /// <summary>JSON string del Dictionary&lt;string, List&lt;string&gt;&gt; de localidades seleccionadas.</summary>
    public string LocalidadesJson { get; set; } = "{}";

    /// <summary>Lista de departamentos con sus ciudades disponibles (para renderizar el formulario).</summary>
    public List<DepartamentoFormItem> DepartamentosInfo { get; set; } = [];

    public Dictionary<string, List<string>> ParsearLocalidades()
    {
        if (string.IsNullOrWhiteSpace(LocalidadesJson)) return [];
        return JsonSerializer.Deserialize<Dictionary<string, List<string>>>(LocalidadesJson) ?? [];
    }
}
