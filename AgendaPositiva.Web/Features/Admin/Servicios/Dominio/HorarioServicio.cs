using AgendaPositiva.Web.Features.Commons.Domain;

namespace AgendaPositiva.Web.Features.Admin.Servicios.Dominio;

public class HorarioServicio : EntidadBase
{
    public int ServicioId { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public DateTime FechaHoraInicio { get; set; }
    public DateTime FechaHoraFin { get; set; }
    public Servicio Servicio { get; set; } = null!;
    public ICollection<GrupoServicio> Grupos { get; set; } = [];
}
