using AgendaPositiva.Web.Features.Commons.Domain;

namespace AgendaPositiva.Web.Features.Admin.Hospedajes.Dominio;

public class Hotel : EntidadBase
{
    public string Nombre { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;

    public ICollection<HabitacionHotel> Habitaciones { get; set; } = [];
}
