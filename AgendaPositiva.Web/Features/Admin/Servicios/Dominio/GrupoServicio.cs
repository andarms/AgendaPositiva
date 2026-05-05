using AgendaPositiva.Web.Features.Commons.Domain;
using AgendaPositiva.Web.Features.Inscripciones.Dominio;

namespace AgendaPositiva.Web.Features.Admin.Servicios.Dominio;

public class GrupoServicio : EntidadBase
{
    public string Nombre { get; set; } = string.Empty;
    public int ServicioId { get; set; }
    public int HorarioServicioId { get; set; }
    public int? LiderInscripcionId { get; set; }
    public Servicio Servicio { get; set; } = null!;
    public HorarioServicio HorarioServicio { get; set; } = null!;
    public Inscripcion? LiderInscripcion { get; set; }
    public ICollection<MiembroGrupoServicio> Miembros { get; set; } = [];
}
