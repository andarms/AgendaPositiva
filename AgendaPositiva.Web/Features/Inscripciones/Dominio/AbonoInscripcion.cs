using AgendaPositiva.Web.Features.Commons.Domain;

namespace AgendaPositiva.Web.Features.Inscripciones.Dominio;

public class AbonoInscripcion : EntidadBase
{
    public int InscripcionId { get; set; }
    public decimal Monto { get; set; }
    public string? Observaciones { get; set; }
    public string RegistradoPorUsuario { get; set; } = string.Empty;

    // Navigation
    public Inscripcion Inscripcion { get; set; } = null!;
}
