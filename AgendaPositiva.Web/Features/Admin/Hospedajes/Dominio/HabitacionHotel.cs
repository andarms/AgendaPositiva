using AgendaPositiva.Web.Features.Commons.Domain;

namespace AgendaPositiva.Web.Features.Admin.Hospedajes.Dominio;

public class HabitacionHotel : EntidadBase
{
    public int HotelId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int CamasSencillas { get; set; }
    public int CamasDobles { get; set; }

    public Hotel Hotel { get; set; } = null!;
    public ICollection<AsignacionHospedaje> Asignaciones { get; set; } = [];

    public int Capacidad => CamasSencillas + CamasDobles * 2;
}
