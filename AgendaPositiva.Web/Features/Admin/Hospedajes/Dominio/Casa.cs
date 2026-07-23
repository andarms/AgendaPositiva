using AgendaPositiva.Web.Features.Commons.Domain;
using AgendaPositiva.Web.Features.Inscripciones.Dominio;

namespace AgendaPositiva.Web.Features.Admin.Hospedajes.Dominio;

public class Casa : EntidadBase
{
    public string Nombre { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string NombreResponsable { get; set; } = string.Empty;
    public string TelefonoResponsable { get; set; } = string.Empty;
    public int CuposSolteros { get; set; }
    public int CuposSolteras { get; set; }
    public int CuposParejas { get; set; }
    public int? ResponsablePersonaId { get; set; }
    public bool Activa { get; set; } = true;

    public Persona? ResponsablePersona { get; set; }
    public ICollection<AsignacionHospedaje> Asignaciones { get; set; } = [];

    public int CapacidadTotal => CuposSolteros + CuposSolteras + CuposParejas * 2;
}
