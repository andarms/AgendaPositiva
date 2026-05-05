using AgendaPositiva.Web.Features.Commons.Domain;
using AgendaPositiva.Web.Features.Inscripciones.Dominio;

namespace AgendaPositiva.Web.Features.Admin.Servicios.Dominio;

public class MiembroGrupoServicio : EntidadBase
{
    public int GrupoServicioId { get; set; }
    public int InscripcionId { get; set; }
    public GrupoServicio GrupoServicio { get; set; } = null!;
    public Inscripcion Inscripcion { get; set; } = null!;
}
