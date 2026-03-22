using AgendaPositiva.Web.Features.Commons;
using AgendaPositiva.Web.Features.Commons.Domain;

namespace AgendaPositiva.Web.Features.Inscripciones.Dominio;

public class Inscripcion : EntidadBase
{
    public int PersonaId { get; set; }
    public int EventoId { get; set; }
    public bool RequiereHospedaje { get; set; } = false;
    public int? GrupoAsistenciaId { get; set; }

    /// <summary>Relación con el líder del grupo.</summary>
    public RelacionConLider? RelacionConLider { get; set; }

    /// <summary>Estado actual de la inscripción.</summary>
    public EstadoInscripcion Estado { get; set; } = EstadoInscripcion.Pendiente;
    public string? NecesidadesEspeciales { get; set; }
    public string Localidad { get; set; } = string.Empty;

    // Navigation properties
    public Persona Persona { get; set; } = null!;
    public Evento Evento { get; set; } = null!;
    public GrupoFamiliar? GrupoAsistencia { get; set; }
}