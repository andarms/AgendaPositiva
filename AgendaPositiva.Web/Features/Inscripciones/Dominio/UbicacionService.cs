using System.Text.Json;

namespace AgendaPositiva.Web.Features.Inscripciones.Dominio;

public class UbicacionService
{
    public const string Internacional = "Internacional";
    public const string Misioneros = "Misioneros";

    public List<DepartamentoInfo> Departamentos { get; }
    readonly JsonSerializerOptions options = new() { PropertyNameCaseInsensitive = true };

    public UbicacionService(IWebHostEnvironment env)
    {
        var path = Path.Combine(env.WebRootPath, "colombia.min.json");
        var json = File.ReadAllText(path);
        var datos = JsonSerializer.Deserialize<List<DepartamentoInfo>>(json, options) ?? [];

        // Agregar opción especial "Internacional" sin ciudades
        datos.Add(new DepartamentoInfo { Departamento = Internacional, Ciudades = [] });

        // Agregar departamento "Misioneros" con ciudades CEPEV y PAC
        datos.Add(new DepartamentoInfo { Departamento = Misioneros, Ciudades = ["CEPEV", "PAC"] });

        datos = [.. datos.OrderBy(d => d.Departamento == Internacional || d.Departamento == Misioneros ? 0 : 1).ThenBy(d => d.Departamento)];

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

    public static List<string> ObtenerTodosLosDepartamentos(IWebHostEnvironment env)
    {
        var path = Path.Combine(env.WebRootPath, "colombia.min.json");
        var json = File.ReadAllText(path);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var datos = JsonSerializer.Deserialize<List<DepartamentoInfo>>(json, options) ?? [];
        return datos.Select(d => d.Departamento).ToList();
    }
}

public class DepartamentoInfo
{
    public int Id { get; set; }
    public string Departamento { get; set; } = string.Empty;
    public List<string> Ciudades { get; set; } = [];
}
