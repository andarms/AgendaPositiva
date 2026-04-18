using AgendaPositiva.Web.Datos;
using AgendaPositiva.Web.Features.Admin.Regiones.Dominio;
using AgendaPositiva.Web.Features.Inscripciones.Dominio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AgendaPositiva.Web.Features.Admin.Regiones;

[Route("admin/regiones")]
[Authorize(Roles = "Administrador")]
public class RegionesAdminController : Controller
{
    readonly AppDbContext store;
    readonly UbicacionService ubicacionService;

    public RegionesAdminController(AppDbContext db, UbicacionService ubicacionService)
    {
        store = db;
        this.ubicacionService = ubicacionService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var evento = await store.Eventos.FirstOrDefaultAsync(e => e.Activo);
        if (evento is null) return View("~/Features/Admin/Regiones/Views/Index.cshtml", new RegionesIndexViewModel());

        var regiones = await store.RegionesEvento
            .Where(r => r.EventoId == evento.Id)
            .OrderBy(r => r.Nombre)
            .ToListAsync();

        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var corteBebe = hoy.AddYears(-3);       // 0-2 años: nacidos después de hace 3 años
        var corteNino = hoy.AddYears(-11);      // 3-10 años: nacidos entre hace 11 y hace 3 años
        var corteAdolescente = hoy.AddYears(-18); // 11-17 años: nacidos entre hace 18 y hace 11 años

        var fechasNacimiento = await store.Inscripciones
            .Where(i => i.EventoId == evento.Id)
            .Select(i => i.Persona.FechaNacimiento)
            .ToListAsync();

        var totalInscripciones = fechasNacimiento.Count;
        var totalBebes = fechasNacimiento.Count(f => f > corteBebe);
        var totalNinos = fechasNacimiento.Count(f => f <= corteBebe && f > corteNino);
        var totalAdolescentes = fechasNacimiento.Count(f => f <= corteNino && f > corteAdolescente);
        var totalAdultos = fechasNacimiento.Count(f => f <= corteAdolescente);

        // Calcular inscritos que no pertenecen a ninguna región
        var inscripcionesLocalidad = await store.Inscripciones
            .Where(i => i.EventoId == evento.Id)
            .Select(i => new { i.Departamento, i.Ciudad })
            .ToListAsync();

        var inscritosSinRegion = inscripcionesLocalidad
            .Count(i => !regiones.Any(r => r.Contiene(i.Departamento, i.Ciudad)));

        var vm = new RegionesIndexViewModel
        {
            Evento = evento,
            Regiones = regiones,
            TotalInscripciones = totalInscripciones,
            TotalBebes = totalBebes,
            TotalNinos = totalNinos,
            TotalAdolescentes = totalAdolescentes,
            TotalAdultos = totalAdultos,
            InscritosSinRegion = inscritosSinRegion
        };

        return View("~/Features/Admin/Regiones/Views/Index.cshtml", vm);
    }

    [HttpPost("cupo-evento")]
    public async Task<IActionResult> ActualizarCupoEvento(int cupoTotal)
    {
        var evento = await store.Eventos.FirstOrDefaultAsync(e => e.Activo);
        if (evento is null) return NotFound();

        if (cupoTotal < 0) cupoTotal = 0;
        evento.CupoTotal = cupoTotal;
        await store.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("reasignar-cupos")]
    public async Task<IActionResult> ReasignarCupos()
    {
        var evento = await store.Eventos.FirstOrDefaultAsync(e => e.Activo);
        if (evento is null) return NotFound();

        var regiones = await store.RegionesEvento
            .Where(r => r.EventoId == evento.Id)
            .ToListAsync();

        // Obtener todas las inscripciones del evento activo
        var inscripciones = await store.Inscripciones
            .Where(i => i.EventoId == evento.Id)
            .Select(i => new { i.Departamento, i.Ciudad })
            .ToListAsync();

        // Recalcular TotalInscritos del evento (todas las inscripciones)
        evento.TotalInscritos = inscripciones.Count;

        // Recalcular TotalInscritos de cada región
        foreach (var region in regiones)
        {
            region.TotalInscritos = inscripciones
                .Count(i => region.Contiene(i.Departamento, i.Ciudad));
        }

        await store.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    RegionFormViewModel CrearViewModel(RegionEvento? region = null)
    {
        var departamentosInfo = ubicacionService.Departamentos
            .Select(d => new DepartamentoRegionItem
            {
                Nombre = d.Departamento,
                CiudadesDisponibles = d.Ciudades
            }).ToList();

        var vm = new RegionFormViewModel
        {
            DepartamentosInfo = departamentosInfo
        };

        if (region is not null)
        {
            vm.Id = region.Id;
            vm.Nombre = region.Nombre;
            vm.Cupo = region.Cupo;
            vm.LocalidadesJson = JsonSerializer.Serialize(region.Localidades);
        }

        return vm;
    }

    [HttpGet("crear")]
    public async Task<IActionResult> Crear()
    {
        var evento = await store.Eventos.FirstOrDefaultAsync(e => e.Activo);
        if (evento is null) return RedirectToAction(nameof(Index));

        var vm = CrearViewModel();
        return View("~/Features/Admin/Regiones/Views/Formulario.cshtml", vm);
    }

    [HttpPost("crear")]
    public async Task<IActionResult> CrearPost(RegionFormViewModel vm)
    {
        var evento = await store.Eventos.FirstOrDefaultAsync(e => e.Activo);
        if (evento is null) return RedirectToAction(nameof(Index));

        if (!ModelState.IsValid)
        {
            var vmErr = CrearViewModel();
            vmErr.Nombre = vm.Nombre;
            vmErr.Cupo = vm.Cupo;
            vmErr.LocalidadesJson = vm.LocalidadesJson;
            return View("~/Features/Admin/Regiones/Views/Formulario.cshtml", vmErr);
        }

        var region = new RegionEvento
        {
            Nombre = vm.Nombre,
            EventoId = evento.Id,
            Cupo = vm.Cupo,
            Localidades = vm.ParsearLocalidades()
        };

        store.RegionesEvento.Add(region);
        await store.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("editar/{id:int}")]
    public async Task<IActionResult> Editar(int id)
    {
        var region = await store.RegionesEvento.FindAsync(id);
        if (region is null) return NotFound();

        var vm = CrearViewModel(region);
        return View("~/Features/Admin/Regiones/Views/Formulario.cshtml", vm);
    }

    [HttpPost("editar/{id:int}")]
    public async Task<IActionResult> EditarPost(int id, RegionFormViewModel vm)
    {
        var region = await store.RegionesEvento.FindAsync(id);
        if (region is null) return NotFound();

        if (!ModelState.IsValid)
        {
            var vmErr = CrearViewModel(region);
            vmErr.Nombre = vm.Nombre;
            vmErr.Cupo = vm.Cupo;
            vmErr.LocalidadesJson = vm.LocalidadesJson;
            return View("~/Features/Admin/Regiones/Views/Formulario.cshtml", vmErr);
        }

        region.Nombre = vm.Nombre;
        region.Cupo = vm.Cupo;
        region.Localidades = vm.ParsearLocalidades();

        await store.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("sin-region")]
    public async Task<IActionResult> SinRegion()
    {
        var evento = await store.Eventos.FirstOrDefaultAsync(e => e.Activo);
        if (evento is null) return RedirectToAction(nameof(Index));

        var regiones = await store.RegionesEvento
            .Where(r => r.EventoId == evento.Id)
            .ToListAsync();

        var inscripciones = await store.Inscripciones
            .Include(i => i.Persona)
            .Where(i => i.EventoId == evento.Id)
            .ToListAsync();

        var sinRegion = inscripciones
            .Where(i => !regiones.Any(r => r.Contiene(i.Departamento, i.Ciudad)))
            .OrderBy(i => i.Departamento)
            .ThenBy(i => i.Ciudad)
            .ToList();

        return View("~/Features/Admin/Regiones/Views/SinRegion.cshtml", sinRegion);
    }

    [HttpPost("eliminar/{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var region = await store.RegionesEvento.FindAsync(id);
        if (region is null) return NotFound();

        store.RegionesEvento.Remove(region);
        await store.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}

public class RegionesIndexViewModel
{
    public Evento? Evento { get; set; }
    public List<RegionEvento> Regiones { get; set; } = [];
    public int TotalInscripciones { get; set; }
    public int TotalBebes { get; set; }
    public int TotalNinos { get; set; }
    public int TotalAdolescentes { get; set; }
    public int TotalAdultos { get; set; }
    public int InscritosSinRegion { get; set; }
}

public class DepartamentoRegionItem
{
    public string Nombre { get; set; } = string.Empty;
    public List<string> CiudadesDisponibles { get; set; } = [];
}

public class RegionFormViewModel
{
    public int? Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Cupo { get; set; } = 100;

    /// <summary>JSON string del Dictionary&lt;string, List&lt;string&gt;&gt; de localidades.</summary>
    public string LocalidadesJson { get; set; } = "{}";

    public List<DepartamentoRegionItem> DepartamentosInfo { get; set; } = [];

    public Dictionary<string, List<string>> ParsearLocalidades()
    {
        if (string.IsNullOrWhiteSpace(LocalidadesJson)) return [];
        return JsonSerializer.Deserialize<Dictionary<string, List<string>>>(LocalidadesJson) ?? [];
    }
}
