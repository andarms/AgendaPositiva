using AgendaPositiva.Web.Features.Commons.Domain;

namespace AgendaPositiva.Web.Features.Inscripciones.Dominio;

public class GrupoFamiliar : EntidadBase
{
    // Navigation properties
    public ICollection<Inscripcion> Inscripciones { get; set; } = [];
}