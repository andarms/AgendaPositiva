using AgendaPositiva.Web.Features.Inscripciones.Dominio;
using Microsoft.AspNetCore.Mvc;

namespace AgendaPositiva.Web.Features.Inscripciones;

[Route("api/ubicacion")]
[ApiController]
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
        var ciudades = ubicacion.ObtenerCiudades(departamento);
        return Ok(ciudades);
    }
}
