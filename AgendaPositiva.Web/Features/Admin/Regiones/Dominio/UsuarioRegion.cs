using AgendaPositiva.Web.Features.Admin.Auth.Domain;
using AgendaPositiva.Web.Features.Commons.Domain;

namespace AgendaPositiva.Web.Features.Admin.Regiones.Dominio;

/// <summary>
/// Tabla de relación entre UsuarioAdministrador y RegionEvento.
/// Los administradores tienen acceso a todas las regiones; los colaboradores solo a las asignadas.
/// </summary>
public class UsuarioRegion : EntidadBase
{
    public int UsuarioAdministradorId { get; set; }
    public int RegionEventoId { get; set; }

    // Navigation
    public UsuarioAdministrador Usuario { get; set; } = null!;
    public RegionEvento Region { get; set; } = null!;
}
