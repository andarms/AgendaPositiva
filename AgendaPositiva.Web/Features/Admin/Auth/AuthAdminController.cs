using System.Security.Claims;
using System.Text.Json;
using AgendaPositiva.Web.Datos;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgendaPositiva.Web.Features.Admin.Auth;

[Route("admin/auth")]
public class AuthAdminController : Controller
{
    readonly AppDbContext store;

    public AuthAdminController(AppDbContext db)
    {
        store = db;
    }

    [HttpGet("login")]
    [HttpGet("/admin")]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
            return Redirect("/admin/inscripciones");

        return View("~/Features/Admin/Auth/Views/Login.cshtml");
    }

    [HttpGet("login/google")]
    public IActionResult LoginGoogle()
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(GoogleCallback))
        };
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("google-callback")]
    [Authorize]
    public async Task<IActionResult> GoogleCallback()
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        var nombre = User.FindFirstValue(ClaimTypes.Name);

        if (string.IsNullOrEmpty(email))
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return View("~/Features/Admin/Auth/Views/AccesoDenegado.cshtml");
        }

        var usuario = await store.UsuariosAdministradores
            .FirstOrDefaultAsync(u => u.Email == email && u.Activo);

        if (usuario is null)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return View("~/Features/Admin/Auth/Views/AccesoDenegado.cshtml",
                new AccesoDenegadoViewModel { Email = email });
        }

        // Crear claims con rol real del usuario
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Name, nombre ?? usuario.Nombre ?? email),
            new("AdminUsuarioId", usuario.Id.ToString()),
            new(ClaimTypes.Role, usuario.Rol.ToString()),
            new("Localidades", JsonSerializer.Serialize(usuario.Localidades))
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        return RedirectToAction("Index", "InscripcionesAdmin");
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Landing");
    }
}

public class AccesoDenegadoViewModel
{
    public string Email { get; set; } = string.Empty;
}
