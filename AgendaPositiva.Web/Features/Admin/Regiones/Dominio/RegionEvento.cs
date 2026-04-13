using AgendaPositiva.Web.Features.Commons.Domain;
using AgendaPositiva.Web.Features.Inscripciones.Dominio;

namespace AgendaPositiva.Web.Features.Admin.Regiones.Dominio;

/// <summary>
/// Región con cupo de inscripciones para un evento.
/// Las localidades siguen el mismo patrón que UsuarioAdministrador:
/// Clave = departamento, Valor = lista de ciudades (vacía = todas las ciudades del departamento).
/// </summary>
public class RegionEvento : EntidadBase
{
    public string Nombre { get; set; } = string.Empty;
    public int EventoId { get; set; }
    public int Cupo { get; set; }
    public int TotalInscritos { get; set; } = 0;

    /// <summary>
    /// Clave = departamento, Valor = lista de ciudades permitidas.
    /// Lista vacía = todas las ciudades del departamento.
    /// </summary>
    public Dictionary<string, List<string>> Localidades { get; set; } = [];

    // Navigation
    public Evento Evento { get; set; } = null!;
    public ICollection<UsuarioRegion> UsuarioRegiones { get; set; } = [];

    public int CupoDisponible => Cupo - TotalInscritos;
    public bool TieneCupo => CupoDisponible > 0;

    /// <summary>
    /// Verifica si una inscripción con el departamento y ciudad dados pertenece a esta región.
    /// </summary>
    public bool Contiene(string departamento, string ciudad)
    {
        if (!Localidades.TryGetValue(departamento, out var ciudades))
            return false;

        // Lista vacía = todas las ciudades del departamento
        if (ciudades.Count == 0)
            return true;

        return ciudades.Contains(ciudad, StringComparer.OrdinalIgnoreCase);
    }
}
