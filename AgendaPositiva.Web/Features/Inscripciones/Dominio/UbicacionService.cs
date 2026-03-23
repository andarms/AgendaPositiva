using System.Text.Json;

namespace AgendaPositiva.Web.Features.Inscripciones.Dominio;

public class UbicacionService
{
    public const string Internacional = "Internacional";

    public List<DepartamentoInfo> Departamentos { get; }
    readonly JsonSerializerOptions options = new() { PropertyNameCaseInsensitive = true };

    public UbicacionService(IWebHostEnvironment env)
    {
        var path = Path.Combine(env.WebRootPath, "colombia.min.json");
        var json = File.ReadAllText(path);
        var datos = JsonSerializer.Deserialize<List<DepartamentoInfo>>(json, options) ?? [];

        // Agregar opción especial "Internacional" sin ciudades
        datos.Add(new DepartamentoInfo { Departamento = Internacional, Ciudades = [] });
        datos = [.. datos.OrderBy(d => d.Departamento == Internacional ? 0 : 1).ThenBy(d => d.Departamento)];

        Departamentos = datos;
    }

    public List<string> ObtenerCiudades(string departamento)
    {
        return Departamentos
            .FirstOrDefault(d => d.Departamento.Equals(departamento, StringComparison.OrdinalIgnoreCase))
            ?.Ciudades ?? [];
    }

    public List<string> ObtenerNombresDepartamentos()
    {
        return Departamentos.Select(d => d.Departamento).ToList();
    }
}

public class DepartamentoInfo
{
    public int Id { get; set; }
    public string Departamento { get; set; } = string.Empty;
    public List<string> Ciudades { get; set; } = [];
}
