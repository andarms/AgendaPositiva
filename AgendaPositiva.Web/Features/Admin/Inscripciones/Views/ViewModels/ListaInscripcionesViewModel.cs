using AgendaPositiva.Web.Features.Commons;
using AgendaPositiva.Web.Features.Inscripciones.Dominio;

namespace AgendaPositiva.Web.Features.Admin.Inscripciones.Views.ViewModels;

public class ListaInscripcionesViewModel
{
    public required Evento Evento { get; init; }
    public required List<Inscripcion> Inscripciones { get; init; }
    public required int TotalInscripciones { get; init; }
    public required List<string> Departamentos { get; init; }

    // Filtros
    public string? FiltroNombre { get; init; }
    public string? FiltroDepartamento { get; init; }
    public EstadoInscripcion? FiltroEstado { get; init; }
    public bool? FiltroHospedaje { get; init; }

    // Sort
    public string? SortLocalidad { get; init; }

    // Paginación
    public int Pagina { get; init; } = 1;
    public int PorPagina { get; init; } = 50;
    public int TotalPaginas { get; init; }

    public string NextSortLocalidad => SortLocalidad switch
    {
        "asc" => "desc",
        "desc" => "",
        _ => "asc"
    };

    public string SortLocalidadIndicator => SortLocalidad switch
    {
        "asc" => " ▲",
        "desc" => " ▼",
        _ => ""
    };

    public string BuildSortUrl(string? sort)
    {
        var qs = new Dictionary<string, string?>
        {
            ["nombre"] = FiltroNombre,
            ["departamento"] = FiltroDepartamento,
            ["estado"] = FiltroEstado?.ToString(),
            ["hospedaje"] = FiltroHospedaje?.ToString(),
            ["sortLocalidad"] = sort,
            ["porPagina"] = PorPagina.ToString(),
        };
        var pairs = qs
            .Where(kv => !string.IsNullOrEmpty(kv.Value))
            .Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value!)}");
        var query = string.Join("&", pairs);
        return "/admin/inscripciones" + (query.Length > 0 ? "?" + query : "");
    }

    public string BuildPageUrl(int pagina, int? porPaginaOverride = null)
    {
        var qs = new Dictionary<string, string?>
        {
            ["nombre"] = FiltroNombre,
            ["departamento"] = FiltroDepartamento,
            ["estado"] = FiltroEstado?.ToString(),
            ["hospedaje"] = FiltroHospedaje?.ToString(),
            ["sortLocalidad"] = SortLocalidad,
            ["pagina"] = pagina.ToString(),
            ["porPagina"] = (porPaginaOverride ?? PorPagina).ToString(),
        };
        var pairs = qs
            .Where(kv => !string.IsNullOrEmpty(kv.Value))
            .Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value!)}");
        var query = string.Join("&", pairs);
        return "/admin/inscripciones" + (query.Length > 0 ? "?" + query : "");
    }
}
