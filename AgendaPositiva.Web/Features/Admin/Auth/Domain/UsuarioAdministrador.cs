using AgendaPositiva.Web.Features.Admin.Regiones.Dominio;
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
    /// <summary>
    /// Clave = departamento, Valor = lista de ciudades permitidas.
    /// Lista vacía = acceso a TODAS las ciudades del departamento.
    /// </summary>
    public Dictionary<string, List<string>> Localidades { get; set; } = [];

    // Navigation
    public ICollection<UsuarioRegion> UsuarioRegiones { get; set; } = [];
}