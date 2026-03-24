using AgendaPositiva.Web.Features.Commons.Domain;

namespace AgendaPositiva.Web.Features.Admin.Auth.Domain;

public enum RolAdministrador
{
    Administrador,
    Colaborador
}

public class UsuarioAdministrador : EntidadBase
{
    public string Email { get; set; } = string.Empty;
    public string? Nombre { get; set; }
    public bool Activo { get; set; } = true;
    public RolAdministrador Rol { get; set; } = RolAdministrador.Colaborador;
    public List<string> Departamentos { get; set; } = [];
}