using AgendaPositiva.Web.Features.Inscripciones.Dominio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace AgendaPositiva.Web.Features.Admin.Inscripciones;

[Route("api/ubicacion")]
[ApiController]
[Authorize(Policy = "AdminPanel")]
public class UbicacionController : ControllerBase
{
    readonly UbicacionService ubicacion;

    public UbicacionController(UbicacionService ubicacion)
    {
        this.ubicacion = ubicacion;
    }

    [HttpGet("ciudades")]
    public IActionResult ObtenerCiudades([FromQuery] string departamento)
    {
        var todasCiudades = ubicacion.ObtenerCiudades(departamento);

        if (!User.IsInRole("Administrador"))
        {
            var json = User.FindFirstValue("Localidades");
            var localidades = string.IsNullOrEmpty(json)
                ? new Dictionary<string, List<string>>()
                : JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json) ?? [];

            if (localidades.TryGetValue(departamento, out var ciudadesPermitidas) && ciudadesPermitidas.Count > 0)
            {
                todasCiudades = todasCiudades.Where(c => ciudadesPermitidas.Contains(c)).ToList();
            }
        }

        return Ok(todasCiudades);
    }
}
