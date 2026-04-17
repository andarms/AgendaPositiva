using AgendaPositiva.Web.Features.Commons;

namespace AgendaPositiva.Web.Features.Inscripciones.Dominio;

/// <summary>
/// Preguntas adicionales exclusivas para niños de 4 a 10 años.
/// Se almacena como JSONB en la tabla de inscripciones.
/// </summary>
public class PreguntasAdicionalesNino
{
    /// <summary>¿El niño participará de la conferencia de FV KIDS?</summary>
    public bool ParticipaFvKids { get; set; }
    /// <summary>Tipo de sangre del niño.</summary>
    public TipoSangre? TipoSangre { get; set; }
    /// <summary>EPS (Entidad Promotora de Salud) del niño.</summary>
    public string? Eps { get; set; }
}
