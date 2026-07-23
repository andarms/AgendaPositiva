using AgendaPositiva.Web.Datos;
using AgendaPositiva.Web.Features.Admin.Hospedajes.Dominio;
using AgendaPositiva.Web.Features.Commons;
using AgendaPositiva.Web.Features.Inscripciones.Dominio;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgendaPositiva.Web.Features.Admin.Hospedajes;

[Route("admin/hospedajes")]
[Authorize(Roles = "Administrador")]
public class HospedajesAdminController : Controller
{
    readonly AppDbContext store;

    public HospedajesAdminController(AppDbContext db) => store = db;

    // ── Dashboard ──────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var casas = await store.Casas.Include(c => c.Asignaciones).OrderBy(c => c.Nombre).ToListAsync();
        var hoteles = await store.Hoteles.Include(h => h.Habitaciones).ThenInclude(h => h.Asignaciones).OrderBy(h => h.Nombre).ToListAsync();

        return View("~/Features/Admin/Hospedajes/Views/Index.cshtml", new DashboardViewModel { Casas = casas, Hoteles = hoteles });
    }

    // ── Casas CRUD ─────────────────────────────────────────────────
    [HttpGet("casas/nueva")]
    public IActionResult CasaFormulario() =>
        View("~/Features/Admin/Hospedajes/Views/CasaFormulario.cshtml", new CasaFormularioViewModel());

    [HttpGet("casas/{id:int}/editar")]
    public async Task<IActionResult> CasaEditar(int id)
    {
        var casa = await store.Casas.FindAsync(id);
        if (casa is null) return NotFound();
        return View("~/Features/Admin/Hospedajes/Views/CasaFormulario.cshtml", new CasaFormularioViewModel
        {
            Id = casa.Id,
            Nombre = casa.Nombre,
            Direccion = casa.Direccion,
            Telefono = casa.Telefono,
            NombreResponsable = casa.NombreResponsable,
            TelefonoResponsable = casa.TelefonoResponsable,
            CuposSolteros = casa.CuposSolteros,
            CuposSolteras = casa.CuposSolteras,
            CuposParejas = casa.CuposParejas,
            ResponsablePersonaId = casa.ResponsablePersonaId
        });
    }

    [HttpPost("casas/guardar")]
    public async Task<IActionResult> GuardarCasa(CasaFormularioViewModel vm)
    {
        if (string.IsNullOrWhiteSpace(vm.Nombre)) return BadRequest("Nombre requerido.");

        if (vm.Id is > 0)
        {
            var casa = await store.Casas.FindAsync(vm.Id);
            if (casa is null) return NotFound();
            casa.Nombre = vm.Nombre;
            casa.Direccion = vm.Direccion ?? string.Empty;
            casa.Telefono = vm.Telefono ?? string.Empty;
            casa.NombreResponsable = vm.NombreResponsable ?? string.Empty;
            casa.TelefonoResponsable = vm.TelefonoResponsable ?? string.Empty;
            casa.CuposSolteros = vm.CuposSolteros;
            casa.CuposSolteras = vm.CuposSolteras;
            casa.CuposParejas = vm.CuposParejas;
            casa.ResponsablePersonaId = vm.ResponsablePersonaId;
        }
        else
        {
            store.Casas.Add(new Casa
            {
                Nombre = vm.Nombre,
                Direccion = vm.Direccion ?? string.Empty,
                Telefono = vm.Telefono ?? string.Empty,
                NombreResponsable = vm.NombreResponsable ?? string.Empty,
                TelefonoResponsable = vm.TelefonoResponsable ?? string.Empty,
                CuposSolteros = vm.CuposSolteros,
                CuposSolteras = vm.CuposSolteras,
                CuposParejas = vm.CuposParejas,
                ResponsablePersonaId = vm.ResponsablePersonaId
            });
        }
        await store.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("casas/{id:int}/eliminar")]
    public async Task<IActionResult> EliminarCasa(int id)
    {
        var casa = await store.Casas.FindAsync(id);
        if (casa is not null) { store.Casas.Remove(casa); await store.SaveChangesAsync(); }
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("casas/{id:int}/toggle")]
    public async Task<IActionResult> ToggleCasa(int id)
    {
        var casa = await store.Casas.FindAsync(id);
        if (casa is not null) { casa.Activa = !casa.Activa; await store.SaveChangesAsync(); }
        return RedirectToAction(nameof(Index));
    }

    // ── Hoteles CRUD ───────────────────────────────────────────────
    [HttpGet("hoteles/nuevo")]
    public IActionResult HotelFormulario() =>
        View("~/Features/Admin/Hospedajes/Views/HotelFormulario.cshtml", new HotelFormularioViewModel());

    [HttpGet("hoteles/{id:int}/editar")]
    public async Task<IActionResult> HotelEditar(int id)
    {
        var hotel = await store.Hoteles.FindAsync(id);
        if (hotel is null) return NotFound();
        return View("~/Features/Admin/Hospedajes/Views/HotelFormulario.cshtml", new HotelFormularioViewModel
        {
            Id = hotel.Id,
            Nombre = hotel.Nombre,
            Direccion = hotel.Direccion,
            Telefono = hotel.Telefono
        });
    }

    [HttpPost("hoteles/guardar")]
    public async Task<IActionResult> GuardarHotel(HotelFormularioViewModel vm)
    {
        if (string.IsNullOrWhiteSpace(vm.Nombre)) return BadRequest("Nombre requerido.");

        if (vm.Id is > 0)
        {
            var hotel = await store.Hoteles.FindAsync(vm.Id);
            if (hotel is null) return NotFound();
            hotel.Nombre = vm.Nombre;
            hotel.Direccion = vm.Direccion ?? string.Empty;
            hotel.Telefono = vm.Telefono ?? string.Empty;
        }
        else
        {
            store.Hoteles.Add(new Hotel
            {
                Nombre = vm.Nombre,
                Direccion = vm.Direccion ?? string.Empty,
                Telefono = vm.Telefono ?? string.Empty
            });
        }
        await store.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("hoteles/{id:int}/eliminar")]
    public async Task<IActionResult> EliminarHotel(int id)
    {
        var hotel = await store.Hoteles.FindAsync(id);
        if (hotel is not null) { store.Hoteles.Remove(hotel); await store.SaveChangesAsync(); }
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("hoteles/{id:int}")]
    public async Task<IActionResult> HotelDetalle(int id)
    {
        var hotel = await store.Hoteles
            .Include(h => h.Habitaciones).ThenInclude(h => h.Asignaciones)
            .FirstOrDefaultAsync(h => h.Id == id);
        if (hotel is null) return NotFound();
        return View("~/Features/Admin/Hospedajes/Views/HotelDetalle.cshtml", hotel);
    }

    // ── Habitaciones ───────────────────────────────────────────────
    [HttpGet("hoteles/{hotelId:int}/habitaciones/nueva")]
    public async Task<IActionResult> HabitacionFormulario(int hotelId)
    {
        var hotel = await store.Hoteles.FindAsync(hotelId);
        if (hotel is null) return NotFound();
        return View("~/Features/Admin/Hospedajes/Views/HabitacionFormulario.cshtml",
            new HabitacionFormularioViewModel { HotelId = hotelId, HotelNombre = hotel.Nombre });
    }

    [HttpGet("habitaciones/{id:int}/editar")]
    public async Task<IActionResult> HabitacionEditar(int id)
    {
        var hab = await store.HabitacionesHotel.Include(h => h.Hotel).FirstOrDefaultAsync(h => h.Id == id);
        if (hab is null) return NotFound();
        return View("~/Features/Admin/Hospedajes/Views/HabitacionFormulario.cshtml", new HabitacionFormularioViewModel
        {
            Id = hab.Id,
            HotelId = hab.HotelId,
            HotelNombre = hab.Hotel.Nombre,
            Nombre = hab.Nombre,
            CamasSencillas = hab.CamasSencillas,
            CamasDobles = hab.CamasDobles
        });
    }

    [HttpPost("habitaciones/guardar")]
    public async Task<IActionResult> GuardarHabitacion(HabitacionFormularioViewModel vm)
    {
        if (string.IsNullOrWhiteSpace(vm.Nombre)) return BadRequest("Nombre requerido.");

        if (vm.Id is > 0)
        {
            var hab = await store.HabitacionesHotel.FindAsync(vm.Id);
            if (hab is null) return NotFound();
            hab.Nombre = vm.Nombre;
            hab.CamasSencillas = vm.CamasSencillas;
            hab.CamasDobles = vm.CamasDobles;
        }
        else
        {
            store.HabitacionesHotel.Add(new HabitacionHotel
            {
                HotelId = vm.HotelId,
                Nombre = vm.Nombre,
                CamasSencillas = vm.CamasSencillas,
                CamasDobles = vm.CamasDobles
            });
        }
        await store.SaveChangesAsync();
        return RedirectToAction(nameof(HotelDetalle), new { id = vm.HotelId });
    }

    [HttpPost("habitaciones/{id:int}/eliminar")]
    public async Task<IActionResult> EliminarHabitacion(int id)
    {
        var hab = await store.HabitacionesHotel.FindAsync(id);
        if (hab is null) return NotFound();
        var hotelId = hab.HotelId;
        store.HabitacionesHotel.Remove(hab);
        await store.SaveChangesAsync();
        return RedirectToAction(nameof(HotelDetalle), new { id = hotelId });
    }

    // ── Asignaciones ───────────────────────────────────────────────
    [HttpGet("asignaciones")]
    public async Task<IActionResult> Asignaciones(string? nombre, string? documento, string? genero, string? departamento, string? ciudad, string? tab, int pagina = 1, int porPagina = 50)
    {
        var evento = await store.Eventos.FirstOrDefaultAsync(e => e.Activo);
        if (evento is null) return View("~/Features/Admin/Hospedajes/Views/Asignaciones.cshtml",
            new AsignacionesViewModel());

        // Base query: inscritos que requieren hospedaje
        var query = store.Inscripciones
            .Include(i => i.Persona)
            .Where(i => i.EventoId == evento.Id && i.RequiereHospedaje && i.Estado != EstadoInscripcion.NoVaAsistir);

        // Filtros aditivos
        if (!string.IsNullOrWhiteSpace(nombre))
            query = query.Where(i => (i.Persona.Nombres + " " + i.Persona.Apellidos).ToLower().Contains(nombre.Trim().ToLower()));
        if (!string.IsNullOrWhiteSpace(documento))
            query = query.Where(i => i.Persona.NumeroIdentificacion.Contains(documento.Trim()));
        if (!string.IsNullOrWhiteSpace(genero) && Enum.TryParse<Genero>(genero, out var gen))
            query = query.Where(i => i.Persona.Genero == gen);
        if (!string.IsNullOrWhiteSpace(departamento))
            query = query.Where(i => i.Departamento == departamento);
        if (!string.IsNullOrWhiteSpace(ciudad))
            query = query.Where(i => i.Ciudad == ciudad);

        // Tab filter
        var idsAsignados = await store.AsignacionesHospedaje.Select(a => a.InscripcionId).ToListAsync();

        if (tab == "sin-asignar")
            query = query.Where(i => !idsAsignados.Contains(i.Id));
        else if (tab == "asignados")
            query = query.Where(i => idsAsignados.Contains(i.Id));

        // Orden: necesidades especiales primero, luego nombre
        var ordenada = query
            .OrderByDescending(i => i.NecesidadesEspeciales != null && i.NecesidadesEspeciales != "")
            .ThenBy(i => i.Persona.Nombres).ThenBy(i => i.Persona.Apellidos);

        // Paginación
        var allowedSizes = new[] { 10, 50, 100 };
        if (!allowedSizes.Contains(porPagina)) porPagina = 50;
        var total = await ordenada.CountAsync();
        var totalPaginas = (int)Math.Ceiling(total / (double)porPagina);
        if (pagina < 1) pagina = 1;
        if (pagina > totalPaginas && totalPaginas > 0) pagina = totalPaginas;

        var inscripciones = await ordenada.Skip((pagina - 1) * porPagina).Take(porPagina).ToListAsync();

        var asignaciones = await store.AsignacionesHospedaje
            .Include(a => a.Casa)
            .Include(a => a.HabitacionHotel).ThenInclude(h => h!.Hotel)
            .Where(a => inscripciones.Select(i => i.Id).Contains(a.InscripcionId))
            .ToDictionaryAsync(a => a.InscripcionId);

        var casas = await store.Casas.Where(c => c.Activa).Include(c => c.Asignaciones).ToListAsync();
        var hoteles = await store.Hoteles.Where(h => h.Activo)
            .Include(h => h.Habitaciones).ThenInclude(h => h.Asignaciones).ToListAsync();

        // Listas para dropdowns de filtro
        var departamentos = await store.Inscripciones
            .Where(i => i.EventoId == evento.Id && i.RequiereHospedaje)
            .Select(i => i.Departamento).Distinct().OrderBy(d => d).ToListAsync();
        var ciudades = !string.IsNullOrWhiteSpace(departamento)
            ? await store.Inscripciones
                .Where(i => i.EventoId == evento.Id && i.RequiereHospedaje && i.Departamento == departamento)
                .Select(i => i.Ciudad).Where(c => c != "").Distinct().OrderBy(c => c).ToListAsync()
            : [];

        return View("~/Features/Admin/Hospedajes/Views/Asignaciones.cshtml", new AsignacionesViewModel
        {
            Inscripciones = inscripciones,
            Asignaciones = asignaciones,
            Casas = casas,
            Hoteles = hoteles,
            Nombre = nombre,
            Documento = documento,
            Genero = genero,
            Departamento = departamento,
            Ciudad = ciudad,
            Tab = tab ?? "todos",
            Pagina = pagina,
            PorPagina = porPagina,
            Total = total,
            TotalPaginas = totalPaginas,
            Departamentos = departamentos,
            Ciudades = ciudades
        });
    }

    [HttpPost("asignaciones/asignar")]
    public async Task<IActionResult> Asignar(int inscripcionId, int? casaId, TipoCupoCasa? tipoCupo, int? habitacionHotelId)
    {
        var inscripcion = await store.Inscripciones.Include(i => i.Persona).FirstOrDefaultAsync(i => i.Id == inscripcionId);
        if (inscripcion is null) return NotFound();

        // Validación estricta
        if (casaId is not null)
        {
            if (tipoCupo is null) return ErrorAsignacion("Debe seleccionar el tipo de cupo (Soltero, Soltera o Pareja).");
            var casa = await store.Casas.Include(c => c.Asignaciones).FirstOrDefaultAsync(c => c.Id == casaId);
            if (casa is null) return NotFound();

            var genero = inscripcion.Persona.Genero;
            var asignadas = casa.Asignaciones;
            switch (tipoCupo)
            {
                case TipoCupoCasa.Soltero:
                    if (genero != Genero.Masculino)
                        return ErrorAsignacion($"No se puede asignar a \"{inscripcion.Persona.NombreCompleto}\" como soltero porque su género es Femenino.");
                    if (asignadas.Count(a => a.TipoCupoCasa == TipoCupoCasa.Soltero) >= casa.CuposSolteros)
                        return ErrorAsignacion($"La casa \"{casa.Nombre}\" no tiene cupos de solteros disponibles ({casa.CuposSolteros}/{casa.CuposSolteros} ocupados).");
                    break;
                case TipoCupoCasa.Soltera:
                    if (genero != Genero.Femenino)
                        return ErrorAsignacion($"No se puede asignar a \"{inscripcion.Persona.NombreCompleto}\" como soltera porque su género es Masculino.");
                    if (asignadas.Count(a => a.TipoCupoCasa == TipoCupoCasa.Soltera) >= casa.CuposSolteras)
                        return ErrorAsignacion($"La casa \"{casa.Nombre}\" no tiene cupos de solteras disponibles ({casa.CuposSolteras}/{casa.CuposSolteras} ocupados).");
                    break;
                case TipoCupoCasa.Pareja:
                    if (asignadas.Count(a => a.TipoCupoCasa == TipoCupoCasa.Pareja) >= casa.CuposParejas * 2)
                        return ErrorAsignacion($"La casa \"{casa.Nombre}\" no tiene cupos de parejas disponibles ({casa.CuposParejas * 2}/{casa.CuposParejas * 2} ocupados).");
                    break;
            }
        }
        else if (habitacionHotelId is not null)
        {
            var hab = await store.HabitacionesHotel.Include(h => h.Asignaciones).Include(h => h.Hotel).FirstOrDefaultAsync(h => h.Id == habitacionHotelId);
            if (hab is null) return NotFound();
            if (hab.Asignaciones.Count >= hab.Capacidad)
                return ErrorAsignacion($"La habitación \"{hab.Nombre}\" del hotel \"{hab.Hotel.Nombre}\" está llena ({hab.Capacidad}/{hab.Capacidad} ocupados).");
        }
        else
        {
            return ErrorAsignacion("Debe seleccionar una casa o habitación de hotel.");
        }

        // Upsert
        var existente = await store.AsignacionesHospedaje.FirstOrDefaultAsync(a => a.InscripcionId == inscripcionId);
        if (existente is not null)
        {
            existente.CasaId = casaId;
            existente.HabitacionHotelId = habitacionHotelId;
            existente.TipoCupoCasa = casaId is not null ? tipoCupo : null;
        }
        else
        {
            store.AsignacionesHospedaje.Add(new AsignacionHospedaje
            {
                InscripcionId = inscripcionId,
                CasaId = casaId,
                HabitacionHotelId = habitacionHotelId,
                TipoCupoCasa = casaId is not null ? tipoCupo : null
            });
        }
        await store.SaveChangesAsync();
        return RedirectToAction(nameof(Asignaciones));
    }

    IActionResult ErrorAsignacion(string mensaje)
    {
        TempData["Error"] = mensaje;
        return RedirectToAction(nameof(Asignaciones));
    }

    [HttpPost("asignaciones/{id:int}/desasignar")]
    public async Task<IActionResult> Desasignar(int id)
    {
        var asig = await store.AsignacionesHospedaje.FindAsync(id);
        if (asig is not null) { store.AsignacionesHospedaje.Remove(asig); await store.SaveChangesAsync(); }
        return RedirectToAction(nameof(Asignaciones));
    }

    // ── Export Excel ───────────────────────────────────────────────
    [HttpGet("asignaciones/exportar")]
    public async Task<IActionResult> ExportarExcel()
    {
        var evento = await store.Eventos.FirstOrDefaultAsync(e => e.Activo);
        if (evento is null) return NotFound();

        var inscripciones = await store.Inscripciones
            .Include(i => i.Persona)
            .Where(i => i.EventoId == evento.Id && i.RequiereHospedaje && i.Estado != EstadoInscripcion.NoVaAsistir)
            .OrderByDescending(i => i.NecesidadesEspeciales != null && i.NecesidadesEspeciales != "")
            .ThenBy(i => i.Persona.Nombres).ThenBy(i => i.Persona.Apellidos)
            .ToListAsync();

        var asignaciones = await store.AsignacionesHospedaje
            .Include(a => a.Casa)
            .Include(a => a.HabitacionHotel).ThenInclude(h => h!.Hotel)
            .Where(a => inscripciones.Select(i => i.Id).Contains(a.InscripcionId))
            .ToDictionaryAsync(a => a.InscripcionId);

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Hospedajes");
        var headers = new[] { "Nombre", "Documento", "Edad", "Género", "Teléfono", "Necesidades Especiales", "Lugar Asignado", "Tipo/Habitación" };
        for (int c = 0; c < headers.Length; c++)
            ws.Cell(1, c + 1).Value = headers[c];
        ws.Row(1).Style.Font.Bold = true;

        int row = 2;
        foreach (var insc in inscripciones)
        {
            var p = insc.Persona;
            ws.Cell(row, 1).Value = p.NombreCompleto;
            ws.Cell(row, 2).Value = p.NumeroIdentificacion;
            ws.Cell(row, 3).Value = (int)p.Edad;
            ws.Cell(row, 4).Value = p.Genero.ToString();
            ws.Cell(row, 5).Value = p.Telefono;
            ws.Cell(row, 6).Value = insc.NecesidadesEspeciales ?? "";

            if (asignaciones.TryGetValue(insc.Id, out var asig))
            {
                if (asig.Casa is not null)
                {
                    ws.Cell(row, 7).Value = asig.Casa.Nombre;
                    ws.Cell(row, 8).Value = asig.TipoCupoCasa?.ToString() ?? "";
                }
                else if (asig.HabitacionHotel is not null)
                {
                    ws.Cell(row, 7).Value = asig.HabitacionHotel.Hotel.Nombre;
                    ws.Cell(row, 8).Value = asig.HabitacionHotel.Nombre;
                }
            }
            else
            {
                ws.Cell(row, 7).Value = "Sin asignar";
            }
            row++;
        }
        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "hospedajes.xlsx");
    }

    // ── API: buscar personas para autollenado responsable ──────────
    [HttpGet("api/buscar-personas")]
    public async Task<IActionResult> BuscarPersonas(string q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2) return Json(Array.Empty<object>());
        var evento = await store.Eventos.FirstOrDefaultAsync(e => e.Activo);
        if (evento is null) return Json(Array.Empty<object>());

        var resultados = await store.Inscripciones
            .Include(i => i.Persona)
            .Where(i => i.EventoId == evento.Id && i.Estado != EstadoInscripcion.NoVaAsistir)
            .Where(i => i.Persona.Nombres.Contains(q) || i.Persona.Apellidos.Contains(q) || i.Persona.NumeroIdentificacion.Contains(q))
            .Take(10)
            .Select(i => new { i.Persona.Id, Nombre = i.Persona.Nombres + " " + i.Persona.Apellidos, i.Persona.Telefono })
            .ToListAsync();

        return Json(resultados);
    }
}

// ── ViewModels ─────────────────────────────────────────────────────
public class DashboardViewModel
{
    public List<Casa> Casas { get; set; } = [];
    public List<Hotel> Hoteles { get; set; } = [];
}

public class CasaFormularioViewModel
{
    public int? Id { get; set; }
    public string? Nombre { get; set; }
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public string? NombreResponsable { get; set; }
    public string? TelefonoResponsable { get; set; }
    public int CuposSolteros { get; set; }
    public int CuposSolteras { get; set; }
    public int CuposParejas { get; set; }
    public int? ResponsablePersonaId { get; set; }
}

public class HotelFormularioViewModel
{
    public int? Id { get; set; }
    public string? Nombre { get; set; }
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
}

public class HabitacionFormularioViewModel
{
    public int? Id { get; set; }
    public int HotelId { get; set; }
    public string HotelNombre { get; set; } = string.Empty;
    public string? Nombre { get; set; }
    public int CamasSencillas { get; set; }
    public int CamasDobles { get; set; }
}

public class AsignacionesViewModel
{
    public List<Inscripcion> Inscripciones { get; set; } = [];
    public Dictionary<int, AsignacionHospedaje> Asignaciones { get; set; } = [];
    public List<Casa> Casas { get; set; } = [];
    public List<Hotel> Hoteles { get; set; } = [];

    public string? Nombre { get; set; }
    public string? Documento { get; set; }
    public string? Genero { get; set; }
    public string? Departamento { get; set; }
    public string? Ciudad { get; set; }
    public string Tab { get; set; } = "todos";
    public int Pagina { get; set; } = 1;
    public int PorPagina { get; set; } = 50;
    public int Total { get; set; }
    public int TotalPaginas { get; set; }

    public List<string> Departamentos { get; set; } = [];
    public List<string> Ciudades { get; set; } = [];

    public string BuildPageUrl(int pagina, int? porPaginaOverride = null)
    {
        var qs = new Dictionary<string, string?>
        {
            ["nombre"] = Nombre,
            ["documento"] = Documento,
            ["genero"] = Genero,
            ["departamento"] = Departamento,
            ["ciudad"] = Ciudad,
            ["tab"] = Tab == "todos" ? null : Tab,
            ["pagina"] = pagina.ToString(),
            ["porPagina"] = (porPaginaOverride ?? PorPagina).ToString()
        };
        var query = string.Join("&", qs.Where(kv => !string.IsNullOrEmpty(kv.Value)).Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value!)}"));
        return "/admin/hospedajes/asignaciones" + (query.Length > 0 ? "?" + query : "");
    }
}
