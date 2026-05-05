using AgendaPositiva.Web.Features.Commons.Domain;

namespace AgendaPositiva.Web.Features.Admin.Servicios.Dominio;

public class Servicio : EntidadBase
{
    public string Nombre { get; set; } = string.Empty;
    public int CantidadPersonasRequeridas { get; set; }
    public bool Activo { get; set; } = true;
    public ICollection<HorarioServicio> Horarios { get; set; } = [];
    public ICollection<GrupoServicio> Grupos { get; set; } = [];
}
