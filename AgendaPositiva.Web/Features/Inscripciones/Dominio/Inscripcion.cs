using AgendaPositiva.Web.Features.Commons;
using AgendaPositiva.Web.Features.Commons.Domain;

namespace AgendaPositiva.Web.Features.Inscripciones.Dominio;

public class Inscripcion : EntidadBase
{
    public int PersonaId { get; set; }
    public int EventoId { get; set; }
    public bool RequiereHospedaje { get; set; } = false;
    public int? GrupoFamiliarId { get; set; }
    /// <summary>Parentesco declarado (ej: Esposa, Hijo, Sobrino).</summary>
    public Parentesco? Relacion { get; set; }
    /// <summary>¿Con quién tiene esa relación? (PersonaId)</summary>
    public int? RelacionConPersonaId { get; set; }
    /// <summary>Estado actual de la inscripción. Por defecto, pendiente hasta que luego se registren los pagos.</summary>
    public EstadoInscripcion Estado { get; set; } = EstadoInscripcion.Pendiente;
    public string? NecesidadesEspeciales { get; set; }
    public string Ciudad { get; set; } = string.Empty;
    public string Departamento { get; set; } = string.Empty;

    // Navigation properties
    public Persona Persona { get; set; } = null!;
    public Evento Evento { get; set; } = null!;
    public GrupoFamiliar? GrupoFamiliar { get; set; }
    public Persona? RelacionConPersona { get; set; }

    public string Localidad => string.IsNullOrEmpty(Ciudad) ? Departamento : $"{Departamento}, {Ciudad}";
}