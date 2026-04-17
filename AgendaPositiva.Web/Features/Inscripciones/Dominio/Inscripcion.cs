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
    public List<ServicioInscripcion> Servicios { get; set; } = [];
    public string? NecesidadesEspeciales { get; set; }
    public string Ciudad { get; set; } = string.Empty;
    public string Departamento { get; set; } = string.Empty;
    /// <summary>¿Presenta alguna intolerancia o alergia alimenticia? (Niños 4-10 años)</summary>
    public bool TieneAlergiaAlimentaria { get; set; } = false;
    /// <summary>Descripción de la alergia alimentaria (si aplica)</summary>
    public string? DescripcionAlergia { get; set; }
    /// <summary>¿Va a participar de la comunión de Ancianos, Diácono y Diaconisa?</summary>
    public bool ParticipaComunionAncianos { get; set; } = false;
    /// <summary>¿Requiere el servicio de alimentación que ofrece la conferencia?</summary>
    public bool RequiereAlimentacion { get; set; } = false;
    /// <summary>Preguntas adicionales para niños de 4-10 años (JSONB).</summary>
    public PreguntasAdicionalesNino? PreguntasAdicionalesNino { get; set; }

    // Navigation properties
    public Persona Persona { get; set; } = null!;
    public Evento Evento { get; set; } = null!;
    public GrupoFamiliar? GrupoFamiliar { get; set; }
    public Persona? RelacionConPersona { get; set; }

    public string Localidad => string.IsNullOrEmpty(Ciudad) ? Departamento : $"{Departamento}, {Ciudad}";
}