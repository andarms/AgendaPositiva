using AgendaPositiva.Web.Features.Commons.Domain;

namespace AgendaPositiva.Web.Features.Inscripciones.Dominio;

public class GrupoFamiliar : EntidadBase
{
    /// <summary>Referencia opcional a la persona que lidera el grupo.</summary>
    public int? LiderGrupoId { get; set; }

    // Navigation properties
    public ICollection<Inscripcion> Inscripciones { get; set; } = [];
}