using AgendaPositiva.Web.Features.Commons;
using AgendaPositiva.Web.Features.Commons.Domain;

namespace AgendaPositiva.Web.Features.Inscripciones.Dominio;

public class Persona : EntidadBase
{
    public string Nombres { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public DateOnly FechaNacimiento { get; set; }
    public string Telefono { get; set; } = string.Empty;
    public string? Email { get; set; }
    public TipoIdentificacion TipoIdentificacion { get; set; }
    public string NumeroIdentificacion { get; set; } = string.Empty;
    public ICollection<Inscripcion> Inscripciones { get; set; } = [];
}