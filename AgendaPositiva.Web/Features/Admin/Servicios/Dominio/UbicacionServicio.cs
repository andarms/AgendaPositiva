using AgendaPositiva.Web.Features.Commons.Domain;

namespace AgendaPositiva.Web.Features.Admin.Servicios.Dominio;

public class UbicacionServicio : EntidadBase
{
    public string Nombre { get; set; } = string.Empty;
    public int ServicioId { get; set; }
    public Servicio Servicio { get; set; } = null!;
}
