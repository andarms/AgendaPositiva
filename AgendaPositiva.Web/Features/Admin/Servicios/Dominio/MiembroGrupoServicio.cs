using AgendaPositiva.Web.Features.Commons.Domain;
using AgendaPositiva.Web.Features.Inscripciones.Dominio;

namespace AgendaPositiva.Web.Features.Admin.Servicios.Dominio;

public enum RolMiembroGrupoServicio
{
    Coordinador,
    Lider,
    Servidor
}

public class MiembroGrupoServicio : EntidadBase
{
    public int GrupoServicioId { get; set; }
    public int InscripcionId { get; set; }
    public int? HorarioServicioId { get; set; }
    public int? UbicacionServicioId { get; set; }
    public GrupoServicio GrupoServicio { get; set; } = null!;
    public Inscripcion Inscripcion { get; set; } = null!;
    public HorarioServicio? HorarioServicio { get; set; }
    public UbicacionServicio? UbicacionServicio { get; set; }
    public RolMiembroGrupoServicio Rol { get; set; }
}
