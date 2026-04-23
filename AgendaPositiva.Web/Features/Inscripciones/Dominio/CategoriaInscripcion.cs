using AgendaPositiva.Web.Features.Commons.Domain;

namespace AgendaPositiva.Web.Features.Inscripciones.Dominio;

public class CategoriaInscripcion : EntidadBase
{
    public string Nombre { get; set; } = string.Empty;
    public decimal Precio { get; set; }
    public bool Activa { get; set; } = true;

    // Navigation
    public ICollection<Inscripcion> Inscripciones { get; set; } = [];
}
