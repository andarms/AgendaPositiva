using AgendaPositiva.Web.Features.Commons.Domain;
using AgendaPositiva.Web.Features.Inscripciones.Dominio;

namespace AgendaPositiva.Web.Features.Admin.Hospedajes.Dominio;

public enum TipoCupoCasa
{
    Soltero,
    Soltera,
    Pareja
}

public class AsignacionHospedaje : EntidadBase
{
    public int InscripcionId { get; set; }
    public int? CasaId { get; set; }
    public int? HabitacionHotelId { get; set; }
    public TipoCupoCasa? TipoCupoCasa { get; set; }

    public Inscripcion Inscripcion { get; set; } = null!;
    public Casa? Casa { get; set; }
    public HabitacionHotel? HabitacionHotel { get; set; }
}
