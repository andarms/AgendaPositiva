using AgendaPositiva.Web.Features.Commons.Domain;

namespace AgendaPositiva.Web.Features.Inscripciones.Dominio;

public class Evento : EntidadBase
{
    public string Nombre { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? Descripcion { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public string Ubicacion { get; set; } = string.Empty;

    /// <summary>Solo un evento puede estar activo a la vez.</summary>
    public bool Activo { get; set; } = false;

    // Navigation properties
    public ICollection<Inscripcion> Inscripciones { get; set; } = [];
}