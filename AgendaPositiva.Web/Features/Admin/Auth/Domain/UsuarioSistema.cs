using AgendaPositiva.Web.Features.Commons.Domain;

namespace AgendaPositiva.Web.Features.Admin.Auth.Domain;

public class UsuarioSistema : EntidadBase
{
    public string Email { get; set; } = string.Empty;
    public string? Nombre { get; set; }
    public bool Activo { get; set; } = true;
}