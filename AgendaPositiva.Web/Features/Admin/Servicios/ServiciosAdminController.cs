using AgendaPositiva.Web.Datos;
using AgendaPositiva.Web.Features.Admin.Servicios.Dominio;
using AgendaPositiva.Web.Features.Commons;
using AgendaPositiva.Web.Features.Inscripciones.Dominio;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgendaPositiva.Web.Features.Admin.Servicios;

[Route("admin/servicios")]
[Authorize(Roles = "Administrador,EditorDeServicios,ColaboradorYEditorDeServicios")]
public class ServiciosAdminController : Controller
{
    readonly AppDbContext store;

    public ServiciosAdminController(AppDbContext db) => store = db;

    // ── Dashboard ──────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var servicios = await store.Servicios
            .Include(s => s.Horarios)
            .Include(s => s.Grupos).ThenInclude(g => g.Miembros)
            .OrderBy(s => s.Nombre)
            .ToListAsync();

        var totalGrupos = servicios.Sum(s => s.Grupos.Count);
        var totalMiembros = servicios.Sum(s => s.Grupos.Sum(g => g.Miembros.Count));

        // Alertas: miembros cuya inscripción pasó a NoVaAsistir
        var alertasNoAsiste = await store.MiembrosGrupoServicio
            .Include(m => m.Inscripcion).ThenInclude(i => i.Persona)
            .Include(m => m.GrupoServicio).ThenInclude(g => g.Servicio)
            .Where(m => m.Inscripcion.Estado == EstadoInscripcion.NoVaAsistir)
            .Select(m => new AlertaNoAsisteViewModel
            {
                MiembroId = m.Id,
                NombrePersona = m.Inscripcion.Persona.NombreCompleto,
                NombreGrupo = m.GrupoServicio.Nombre,
                NombreServicio = m.GrupoServicio.Servicio.Nombre
            })
            .ToListAsync();

        // Alertas: inscripciones asignadas a más de un grupo de servicio
        var alertasMultiple = await store.MiembrosGrupoServicio
            .Include(m => m.Inscripcion).ThenInclude(i => i.Persona)
            .Include(m => m.GrupoServicio).ThenInclude(g => g.Servicio)
            .GroupBy(m => m.InscripcionId)
            .Where(g => g.Count() > 1)
            .Select(g => new AlertaMultipleServicioViewModel
            {
                NombrePersona = g.First().Inscripcion.Persona.NombreCompleto,
                Asignaciones = g.Select(m => $"{m.GrupoServicio.Servicio.Nombre} → {m.GrupoServicio.Nombre}").ToList()
            })
            .ToListAsync();

        var vm = new DashboardServiciosViewModel
        {
            Servicios = servicios,
            TotalGrupos = totalGrupos,
            TotalMiembros = totalMiembros,
            AlertasNoAsiste = alertasNoAsiste,
            AlertasMultiple = alertasMultiple
        };

        return View("~/Features/Admin/Servicios/Views/Dashboard.cshtml", vm);
    }

    // ── Listado ────────────────────────────────────────────────────
    [HttpGet("listado")]
    public async Task<IActionResult> Listado()
    {
        var servicios = await store.Servicios
            .Include(s => s.Horarios.OrderBy(h => h.FechaHoraInicio))
            .Include(s => s.Grupos).ThenInclude(g => g.Miembros)
            .OrderBy(s => s.Nombre)
            .ToListAsync();

        return View("~/Features/Admin/Servicios/Views/Listado.cshtml", servicios);
    }

    // ── Crear servicio ─────────────────────────────────────────────
    [HttpGet("crear")]
    public IActionResult Crear() =>
        View("~/Features/Admin/Servicios/Views/Formulario.cshtml", new ServicioFormViewModel());

    [HttpPost("crear")]
    public async Task<IActionResult> CrearPost(ServicioFormViewModel vm)
    {
        if (string.IsNullOrWhiteSpace(vm.Nombre) || vm.CantidadPersonasRequeridas < 1)
        {
            ModelState.AddModelError("", "Nombre y cantidad de personas son requeridos.");
            return View("~/Features/Admin/Servicios/Views/Formulario.cshtml", vm);
        }

        var servicio = new Servicio
        {
            Nombre = vm.Nombre.Trim(),
            CantidadPersonasRequeridas = vm.CantidadPersonasRequeridas
        };

        store.Servicios.Add(servicio);
        await store.SaveChangesAsync();
        TempData["Mensaje"] = "Servicio creado correctamente.";
        return RedirectToAction(nameof(Detalle), new { id = servicio.Id });
    }

    // ── Editar servicio ────────────────────────────────────────────
    [HttpGet("editar/{id:int}")]
    public async Task<IActionResult> Editar(int id)
    {
        var servicio = await store.Servicios.FindAsync(id);
        if (servicio is null) return NotFound();

        var vm = new ServicioFormViewModel
        {
            Id = servicio.Id,
            Nombre = servicio.Nombre,
            CantidadPersonasRequeridas = servicio.CantidadPersonasRequeridas
        };

        return View("~/Features/Admin/Servicios/Views/Formulario.cshtml", vm);
    }

    [HttpPost("editar/{id:int}")]
    public async Task<IActionResult> EditarPost(int id, ServicioFormViewModel vm)
    {
        var servicio = await store.Servicios.FindAsync(id);
        if (servicio is null) return NotFound();

        if (string.IsNullOrWhiteSpace(vm.Nombre) || vm.CantidadPersonasRequeridas < 1)
        {
            ModelState.AddModelError("", "Nombre y cantidad de personas son requeridos.");
            vm.Id = id;
            return View("~/Features/Admin/Servicios/Views/Formulario.cshtml", vm);
        }

        servicio.Nombre = vm.Nombre.Trim();
        servicio.CantidadPersonasRequeridas = vm.CantidadPersonasRequeridas;

        await store.SaveChangesAsync();
        TempData["Mensaje"] = "Servicio actualizado correctamente.";
        return RedirectToAction(nameof(Detalle), new { id });
    }

    // ── Detalle servicio ───────────────────────────────────────────
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detalle(int id)
    {
        var servicio = await store.Servicios
            .Include(s => s.Horarios.OrderBy(h => h.FechaHoraInicio))
            .Include(s => s.Ubicaciones.OrderBy(u => u.Nombre))
            .Include(s => s.Grupos).ThenInclude(g => g.Miembros).ThenInclude(m => m.Inscripcion).ThenInclude(i => i.Persona)
            .Include(s => s.Grupos).ThenInclude(g => g.Miembros).ThenInclude(m => m.HorarioServicio)
            .Include(s => s.Grupos).ThenInclude(g => g.LiderInscripcion).ThenInclude(i => i!.Persona)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (servicio is null) return NotFound();
        return View("~/Features/Admin/Servicios/Views/Detalle.cshtml", servicio);
    }

    // ── Toggle activo ──────────────────────────────────────────────
    [HttpPost("{id:int}/toggle")]
    public async Task<IActionResult> Toggle(int id)
    {
        var servicio = await store.Servicios.FindAsync(id);
        if (servicio is null) return NotFound();

        servicio.Activo = !servicio.Activo;
        await store.SaveChangesAsync();
        return RedirectToAction(nameof(Listado));
    }

    // ── Horarios: listado ──────────────────────────────────────────
    [HttpGet("{servicioId:int}/horarios")]
    public async Task<IActionResult> HorariosIndex(int servicioId)
    {
        var servicio = await store.Servicios
            .Include(s => s.Horarios.OrderBy(h => h.FechaHoraInicio))
            .FirstOrDefaultAsync(s => s.Id == servicioId);
        if (servicio is null) return NotFound();

        var vm = new HorariosPageViewModel
        {
            ServicioId = servicio.Id,
            ServicioNombre = servicio.Nombre,
            Horarios = servicio.Horarios.ToList()
        };
        return View("~/Features/Admin/Servicios/Views/Horarios.cshtml", vm);
    }

    // ── Horarios: crear (GET) ──────────────────────────────────────
    [HttpGet("{servicioId:int}/horarios/crear")]
    public async Task<IActionResult> HorarioCrearGet(int servicioId)
    {
        var servicio = await store.Servicios.FindAsync(servicioId);
        if (servicio is null) return NotFound();

        var vm = new HorarioFormViewModel
        {
            ServicioId = servicio.Id,
            ServicioNombre = servicio.Nombre
        };
        return View("~/Features/Admin/Servicios/Views/HorarioFormulario.cshtml", vm);
    }

    // ── Horarios: crear (POST) ─────────────────────────────────────
    [HttpPost("{servicioId:int}/horarios/crear")]
    public async Task<IActionResult> HorarioCrear(int servicioId, HorarioFormViewModel form)
    {
        var servicio = await store.Servicios.FindAsync(servicioId);
        if (servicio is null) return NotFound();

        form.ServicioId = servicioId;
        form.ServicioNombre = servicio.Nombre;

        if (string.IsNullOrWhiteSpace(form.Descripcion))
        {
            ViewBag.Error = "La descripción es requerida.";
            return View("~/Features/Admin/Servicios/Views/HorarioFormulario.cshtml", form);
        }

        var item = new HorarioFormItem { FechaInicio = form.FechaInicio, HoraInicio = form.HoraInicio, FechaFin = form.FechaFin, HoraFin = form.HoraFin };
        var inicio = item.ObtenerFechaHoraInicio();
        var fin = item.ObtenerFechaHoraFin();

        if (inicio >= fin)
        {
            ViewBag.Error = "La fecha/hora de inicio debe ser anterior a la fecha/hora de fin.";
            return View("~/Features/Admin/Servicios/Views/HorarioFormulario.cshtml", form);
        }

        var existeSolapamiento = await store.HorariosServicio
            .AnyAsync(h => h.ServicioId == servicioId && h.FechaHoraInicio < fin && h.FechaHoraFin > inicio);
        if (existeSolapamiento)
        {
            ViewBag.Error = "Ya existe un horario que se solapa con el rango de fecha y hora indicado.";
            return View("~/Features/Admin/Servicios/Views/HorarioFormulario.cshtml", form);
        }

        store.HorariosServicio.Add(new HorarioServicio
        {
            ServicioId = servicioId,
            Descripcion = form.Descripcion.Trim(),
            FechaHoraInicio = inicio,
            FechaHoraFin = fin
        });
        await store.SaveChangesAsync();
        TempData["Mensaje"] = "Horario agregado.";
        return RedirectToAction(nameof(HorariosIndex), new { servicioId });
    }

    // ── Horarios: editar (GET) ─────────────────────────────────────
    [HttpGet("{servicioId:int}/horarios/{id:int}/editar")]
    public async Task<IActionResult> HorarioEditar(int servicioId, int id)
    {
        var servicio = await store.Servicios.FindAsync(servicioId);
        if (servicio is null) return NotFound();

        var horario = await store.HorariosServicio.FirstOrDefaultAsync(h => h.Id == id && h.ServicioId == servicioId);
        if (horario is null) return NotFound();

        var vm = new HorarioFormViewModel
        {
            ServicioId = servicio.Id,
            ServicioNombre = servicio.Nombre,
            HorarioId = horario.Id,
            Descripcion = horario.Descripcion,
            FechaInicio = horario.FechaHoraInicio.ToString("yyyy-MM-dd"),
            HoraInicio = horario.FechaHoraInicio.ToString("HH:mm"),
            FechaFin = horario.FechaHoraFin.ToString("yyyy-MM-dd"),
            HoraFin = horario.FechaHoraFin.ToString("HH:mm")
        };
        return View("~/Features/Admin/Servicios/Views/HorarioFormulario.cshtml", vm);
    }

    // ── Horarios: editar (POST) ────────────────────────────────────
    [HttpPost("{servicioId:int}/horarios/{id:int}/editar")]
    public async Task<IActionResult> HorarioEditarPost(int servicioId, int id, HorarioFormViewModel form)
    {
        var horario = await store.HorariosServicio.FirstOrDefaultAsync(h => h.Id == id && h.ServicioId == servicioId);
        if (horario is null) return NotFound();

        var servicio = await store.Servicios.FindAsync(servicioId);
        form.ServicioId = servicioId;
        form.ServicioNombre = servicio?.Nombre ?? "";
        form.HorarioId = id;

        if (string.IsNullOrWhiteSpace(form.Descripcion))
        {
            ViewBag.Error = "La descripción es requerida.";
            return View("~/Features/Admin/Servicios/Views/HorarioFormulario.cshtml", form);
        }

        var item = new HorarioFormItem { FechaInicio = form.FechaInicio, HoraInicio = form.HoraInicio, FechaFin = form.FechaFin, HoraFin = form.HoraFin };
        var inicio = item.ObtenerFechaHoraInicio();
        var fin = item.ObtenerFechaHoraFin();

        if (inicio >= fin)
        {
            ViewBag.Error = "La fecha/hora de inicio debe ser anterior a la fecha/hora de fin.";
            return View("~/Features/Admin/Servicios/Views/HorarioFormulario.cshtml", form);
        }

        var existeSolapamiento = await store.HorariosServicio
            .AnyAsync(h => h.ServicioId == servicioId && h.Id != id && h.FechaHoraInicio < fin && h.FechaHoraFin > inicio);
        if (existeSolapamiento)
        {
            ViewBag.Error = "Ya existe un horario que se solapa con el rango de fecha y hora indicado.";
            return View("~/Features/Admin/Servicios/Views/HorarioFormulario.cshtml", form);
        }

        horario.Descripcion = form.Descripcion.Trim();
        horario.FechaHoraInicio = inicio;
        horario.FechaHoraFin = fin;

        await store.SaveChangesAsync();
        TempData["Mensaje"] = "Horario actualizado.";
        return RedirectToAction(nameof(HorariosIndex), new { servicioId });
    }

    // ── Horarios: eliminar ─────────────────────────────────────────
    [HttpPost("{servicioId:int}/horarios/{id:int}/eliminar")]
    public async Task<IActionResult> HorarioEliminar(int servicioId, int id)
    {
        var horario = await store.HorariosServicio.FirstOrDefaultAsync(h => h.Id == id && h.ServicioId == servicioId);
        if (horario is null) return NotFound();

        var tieneMiembros = await store.MiembrosGrupoServicio.AnyAsync(m => m.HorarioServicioId == id);
        if (tieneMiembros)
        {
            TempData["Error"] = "No se puede eliminar el horario porque tiene miembros asignados. Quite el horario de los miembros primero.";
            return RedirectToAction(nameof(HorariosIndex), new { servicioId });
        }

        store.HorariosServicio.Remove(horario);
        await store.SaveChangesAsync();
        TempData["Mensaje"] = "Horario eliminado.";
        return RedirectToAction(nameof(HorariosIndex), new { servicioId });
    }

    // ── Ubicaciones: listado ──────────────────────────────────────
    [HttpGet("{servicioId:int}/ubicaciones")]
    public async Task<IActionResult> UbicacionesIndex(int servicioId)
    {
        var servicio = await store.Servicios
            .Include(s => s.Ubicaciones.OrderBy(u => u.Nombre))
            .FirstOrDefaultAsync(s => s.Id == servicioId);
        if (servicio is null) return NotFound();

        return View("~/Features/Admin/Servicios/Views/Ubicaciones.cshtml", servicio);
    }

    // ── Ubicaciones: crear ─────────────────────────────────────────
    [HttpGet("{servicioId:int}/ubicaciones/crear")]
    public async Task<IActionResult> UbicacionCrear(int servicioId)
    {
        var servicio = await store.Servicios.FindAsync(servicioId);
        if (servicio is null) return NotFound();

        return View("~/Features/Admin/Servicios/Views/UbicacionFormulario.cshtml",
            new UbicacionFormViewModel { ServicioId = servicioId, ServicioNombre = servicio.Nombre });
    }

    [HttpPost("{servicioId:int}/ubicaciones/crear")]
    public async Task<IActionResult> UbicacionCrearPost(int servicioId, UbicacionFormViewModel form)
    {
        var servicio = await store.Servicios.FindAsync(servicioId);
        if (servicio is null) return NotFound();

        form.ServicioId = servicioId;
        form.ServicioNombre = servicio.Nombre;

        if (string.IsNullOrWhiteSpace(form.Nombre))
        {
            ViewBag.Error = "El nombre de la ubicación es requerido.";
            return View("~/Features/Admin/Servicios/Views/UbicacionFormulario.cshtml", form);
        }

        store.UbicacionesServicio.Add(new UbicacionServicio
        {
            ServicioId = servicioId,
            Nombre = form.Nombre.Trim()
        });
        await store.SaveChangesAsync();
        TempData["Mensaje"] = "Ubicación creada.";
        return RedirectToAction(nameof(UbicacionesIndex), new { servicioId });
    }

    // ── Ubicaciones: editar ────────────────────────────────────────
    [HttpGet("{servicioId:int}/ubicaciones/{id:int}/editar")]
    public async Task<IActionResult> UbicacionEditar(int servicioId, int id)
    {
        var servicio = await store.Servicios.FindAsync(servicioId);
        if (servicio is null) return NotFound();

        var ubicacion = await store.UbicacionesServicio.FirstOrDefaultAsync(u => u.Id == id && u.ServicioId == servicioId);
        if (ubicacion is null) return NotFound();

        return View("~/Features/Admin/Servicios/Views/UbicacionFormulario.cshtml",
            new UbicacionFormViewModel
            {
                ServicioId = servicioId,
                ServicioNombre = servicio.Nombre,
                UbicacionId = id,
                Nombre = ubicacion.Nombre
            });
    }

    [HttpPost("{servicioId:int}/ubicaciones/{id:int}/editar")]
    public async Task<IActionResult> UbicacionEditarPost(int servicioId, int id, UbicacionFormViewModel form)
    {
        var ubicacion = await store.UbicacionesServicio.FirstOrDefaultAsync(u => u.Id == id && u.ServicioId == servicioId);
        if (ubicacion is null) return NotFound();

        var servicio = await store.Servicios.FindAsync(servicioId);
        form.ServicioId = servicioId;
        form.ServicioNombre = servicio?.Nombre ?? "";
        form.UbicacionId = id;

        if (string.IsNullOrWhiteSpace(form.Nombre))
        {
            ViewBag.Error = "El nombre de la ubicación es requerido.";
            return View("~/Features/Admin/Servicios/Views/UbicacionFormulario.cshtml", form);
        }

        ubicacion.Nombre = form.Nombre.Trim();
        await store.SaveChangesAsync();
        TempData["Mensaje"] = "Ubicación actualizada.";
        return RedirectToAction(nameof(UbicacionesIndex), new { servicioId });
    }

    // ── Ubicaciones: eliminar ──────────────────────────────────────
    [HttpPost("{servicioId:int}/ubicaciones/{id:int}/eliminar")]
    public async Task<IActionResult> UbicacionEliminar(int servicioId, int id)
    {
        var ubicacion = await store.UbicacionesServicio.FirstOrDefaultAsync(u => u.Id == id && u.ServicioId == servicioId);
        if (ubicacion is null) return NotFound();

        store.UbicacionesServicio.Remove(ubicacion);
        await store.SaveChangesAsync();
        TempData["Mensaje"] = "Ubicación eliminada.";
        return RedirectToAction(nameof(UbicacionesIndex), new { servicioId });
    }

    // ── Grupos: listado ────────────────────────────────────────────
    [HttpGet("{servicioId:int}/grupos")]
    public async Task<IActionResult> GruposIndex(int servicioId)
    {
        var servicio = await store.Servicios
            .Include(s => s.Horarios.OrderBy(h => h.FechaHoraInicio))
            .Include(s => s.Grupos).ThenInclude(g => g.Miembros).ThenInclude(m => m.Inscripcion).ThenInclude(i => i.Persona)
            .Include(s => s.Grupos).ThenInclude(g => g.Miembros).ThenInclude(m => m.HorarioServicio)
            .Include(s => s.Grupos).ThenInclude(g => g.LiderInscripcion).ThenInclude(i => i!.Persona)
            .FirstOrDefaultAsync(s => s.Id == servicioId);

        if (servicio is null) return NotFound();
        return View("~/Features/Admin/Servicios/Views/GruposIndex.cshtml", servicio);
    }

    // ── Grupos: crear ──────────────────────────────────────────────
    [HttpGet("{servicioId:int}/grupos/crear")]
    public async Task<IActionResult> GrupoCrear(int servicioId)
    {
        var servicio = await store.Servicios
            .Include(s => s.Horarios.OrderBy(h => h.FechaHoraInicio))
            .FirstOrDefaultAsync(s => s.Id == servicioId);
        if (servicio is null) return NotFound();

        var vm = await BuildGrupoFormViewModel(servicio);
        return View("~/Features/Admin/Servicios/Views/GrupoFormulario.cshtml", vm);
    }

    [HttpPost("{servicioId:int}/grupos/crear")]
    public async Task<IActionResult> GrupoCrearPost(int servicioId, string nombre)
    {
        var servicio = await store.Servicios
            .Include(s => s.Horarios)
            .FirstOrDefaultAsync(s => s.Id == servicioId);
        if (servicio is null) return NotFound();

        if (string.IsNullOrWhiteSpace(nombre))
        {
            ModelState.AddModelError("", "El nombre es requerido.");
            var vm = await BuildGrupoFormViewModel(servicio);
            vm.Nombre = nombre;
            return View("~/Features/Admin/Servicios/Views/GrupoFormulario.cshtml", vm);
        }

        var grupo = new GrupoServicio
        {
            Nombre = nombre.Trim(),
            ServicioId = servicioId
        };

        store.GruposServicio.Add(grupo);
        await store.SaveChangesAsync();
        return RedirectToAction(nameof(GrupoDetalle), new { id = grupo.Id });
    }

    // ── Grupos: detalle ────────────────────────────────────────────
    [HttpGet("grupos/{id:int}")]
    public async Task<IActionResult> GrupoDetalle(int id)
    {
        var grupo = await store.GruposServicio
            .Include(g => g.Servicio).ThenInclude(s => s.Ubicaciones)
            .Include(g => g.Servicio).ThenInclude(s => s.Horarios)
            .Include(g => g.Miembros).ThenInclude(m => m.Inscripcion).ThenInclude(i => i.Persona)
            .Include(g => g.Miembros).ThenInclude(m => m.HorarioServicio)
            .Include(g => g.Miembros).ThenInclude(m => m.UbicacionServicio)
            .FirstOrDefaultAsync(g => g.Id == id);
        if (grupo is null) return NotFound();

        return View("~/Features/Admin/Servicios/Views/GrupoDetalle.cshtml", grupo);
    }

    // ── Grupos: agregar miembros ───────────────────────────────────
    [HttpGet("grupos/{id:int}/agregar-miembros")]
    public async Task<IActionResult> GrupoAgregarMiembros(int id)
    {
        var grupo = await store.GruposServicio
            .Include(g => g.Servicio)
            .Include(g => g.Miembros)
            .FirstOrDefaultAsync(g => g.Id == id);
        if (grupo is null) return NotFound();

        var idsEnEsteGrupo = grupo.Miembros.Select(m => m.InscripcionId).ToHashSet();
        var evento = await store.Eventos.FirstOrDefaultAsync(e => e.Activo);
        var inscripciones = evento is null
            ? new List<Inscripcion>()
            : await store.Inscripciones
                .Include(i => i.Persona)
                .Where(i => i.EventoId == evento.Id
                    && i.Estado != EstadoInscripcion.NoVaAsistir)
                .OrderBy(i => i.Persona.Nombres).ThenBy(i => i.Persona.Apellidos)
                .ToListAsync();

        // Inscripciones asignadas a otros grupos (para mostrar advertencia)
        var asignacionesOtrosGrupos = await store.MiembrosGrupoServicio
            .Include(m => m.GrupoServicio).ThenInclude(g => g.Servicio)
            .Where(m => m.GrupoServicioId != grupo.Id)
            .Select(m => new { m.InscripcionId, GrupoNombre = m.GrupoServicio.Nombre, ServicioNombre = m.GrupoServicio.Servicio.Nombre })
            .ToListAsync();

        var grupoDeOtros = asignacionesOtrosGrupos
            .GroupBy(a => a.InscripcionId)
            .ToDictionary(
                g => g.Key,
                g => string.Join(", ", g.Select(a => $"{a.ServicioNombre} → {a.GrupoNombre}"))
            );

        return View("~/Features/Admin/Servicios/Views/GrupoAgregarMiembros.cshtml",
            new GrupoAgregarMiembrosViewModel
            {
                GrupoId = grupo.Id,
                ServicioId = grupo.ServicioId,
                ServicioNombre = grupo.Servicio.Nombre,
                GrupoNombre = grupo.Nombre,
                InscripcionesDisponibles = inscripciones,
                IdsYaEnGrupo = idsEnEsteGrupo,
                GrupoDeOtros = grupoDeOtros
            });
    }

    [HttpPost("grupos/{id:int}/agregar-miembros")]
    public async Task<IActionResult> GrupoAgregarMiembrosPost(int id, List<int> miembrosIds)
    {
        var grupo = await store.GruposServicio
            .Include(g => g.Miembros)
            .FirstOrDefaultAsync(g => g.Id == id);
        if (grupo is null) return NotFound();

        var idsExistentes = grupo.Miembros.Select(m => m.InscripcionId).ToHashSet();
        foreach (var mid in (miembrosIds ?? []).Distinct().Where(mid => !idsExistentes.Contains(mid)))
        {
            grupo.Miembros.Add(new MiembroGrupoServicio { InscripcionId = mid, Rol = RolMiembroGrupoServicio.Servidor });
        }

        await store.SaveChangesAsync();
        return RedirectToAction(nameof(GrupoDetalle), new { id });
    }

    // ── Cambiar rol de miembro ─────────────────────────────────────
    [HttpPost("miembros/{id:int}/cambiar-rol")]
    public async Task<IActionResult> CambiarRolMiembro(int id, [FromForm] string rol)
    {
        var miembro = await store.MiembrosGrupoServicio.FindAsync(id);
        if (miembro is null) return NotFound();

        if (!RolMiembroGrupoServicioExtensions.TryParseFlexible(rol, out var nuevoRol))
        {
            return BadRequest("El rol seleccionado no es válido.");
        }

        miembro.Rol = nuevoRol;
        await store.SaveChangesAsync();
        return RedirectToAction(nameof(GrupoDetalle), new { id = miembro.GrupoServicioId });
    }

    // ── Cambiar ubicación de miembro ───────────────────────────────
    [HttpPost("miembros/{id:int}/cambiar-ubicacion")]
    public async Task<IActionResult> CambiarUbicacionMiembro(int id, int? ubicacionServicioId)
    {
        var miembro = await store.MiembrosGrupoServicio.FindAsync(id);
        if (miembro is null) return NotFound();

        miembro.UbicacionServicioId = ubicacionServicioId;
        await store.SaveChangesAsync();
        return RedirectToAction(nameof(GrupoDetalle), new { id = miembro.GrupoServicioId });
    }

    // ── Cambiar horario de miembro ─────────────────────────────────
    [HttpPost("miembros/{id:int}/cambiar-horario")]
    public async Task<IActionResult> CambiarHorarioMiembro(int id, int? horarioServicioId)
    {
        var miembro = await store.MiembrosGrupoServicio.FindAsync(id);
        if (miembro is null) return NotFound();

        miembro.HorarioServicioId = horarioServicioId;
        await store.SaveChangesAsync();
        return RedirectToAction(nameof(GrupoDetalle), new { id = miembro.GrupoServicioId });
    }

    // ── Grupos: editar ─────────────────────────────────────────────
    [HttpGet("grupos/{id:int}/editar")]
    public async Task<IActionResult> GrupoEditar(int id)
    {
        var grupo = await store.GruposServicio
            .Include(g => g.Servicio).ThenInclude(s => s.Horarios.OrderBy(h => h.FechaHoraInicio))
            .FirstOrDefaultAsync(g => g.Id == id);
        if (grupo is null) return NotFound();

        var vm = await BuildGrupoFormViewModel(grupo.Servicio);
        vm.GrupoId = grupo.Id;
        vm.Nombre = grupo.Nombre;
        return View("~/Features/Admin/Servicios/Views/GrupoFormulario.cshtml", vm);
    }

    [HttpPost("grupos/{id:int}/editar")]
    public async Task<IActionResult> GrupoEditarPost(int id, string nombre)
    {
        var grupo = await store.GruposServicio
            .Include(g => g.Servicio).ThenInclude(s => s.Horarios)
            .FirstOrDefaultAsync(g => g.Id == id);
        if (grupo is null) return NotFound();

        if (string.IsNullOrWhiteSpace(nombre))
        {
            ModelState.AddModelError("", "El nombre es requerido.");
            var vm = await BuildGrupoFormViewModel(grupo.Servicio);
            vm.GrupoId = id;
            vm.Nombre = nombre;
            return View("~/Features/Admin/Servicios/Views/GrupoFormulario.cshtml", vm);
        }

        grupo.Nombre = nombre.Trim();

        await store.SaveChangesAsync();
        return RedirectToAction(nameof(GrupoDetalle), new { id });
    }

    // ── Grupos: eliminar ───────────────────────────────────────────
    [HttpPost("grupos/{id:int}/eliminar")]
    public async Task<IActionResult> GrupoEliminar(int id)
    {
        var grupo = await store.GruposServicio.FindAsync(id);
        if (grupo is null) return NotFound();

        var servicioId = grupo.ServicioId;
        store.GruposServicio.Remove(grupo);
        await store.SaveChangesAsync();
        return RedirectToAction(nameof(GruposIndex), new { servicioId });
    }

    // ── Remover miembro ───────────────────────────────────────────
    [HttpPost("miembros/{id:int}/remover")]
    public async Task<IActionResult> RemoverMiembro(int id)
    {
        var miembro = await store.MiembrosGrupoServicio.FindAsync(id);
        if (miembro is null) return NotFound();

        var grupoId = miembro.GrupoServicioId;
        store.MiembrosGrupoServicio.Remove(miembro);
        await store.SaveChangesAsync();
        return RedirectToAction(nameof(GrupoDetalle), new { id = grupoId });
    }

    // ── Excel export ───────────────────────────────────────────────
    [HttpGet("exportar")]
    public async Task<IActionResult> Exportar()
    {
        var servicios = await store.Servicios
            .Include(s => s.Horarios.OrderBy(h => h.FechaHoraInicio))
            .Include(s => s.Grupos).ThenInclude(g => g.Miembros).ThenInclude(m => m.Inscripcion).ThenInclude(i => i.Persona)
            .Include(s => s.Grupos).ThenInclude(g => g.Miembros).ThenInclude(m => m.HorarioServicio)
            .Include(s => s.Grupos).ThenInclude(g => g.Miembros).ThenInclude(m => m.UbicacionServicio)
            .OrderBy(s => s.Nombre)
            .ToListAsync();

        using var workbook = new XLWorkbook();

        // Hoja 1: Servicios y horarios
        var ws1 = workbook.Worksheets.Add("Servicios");
        var headers1 = new[] { "Servicio", "Personas Requeridas", "Estado", "Horario", "Inicio", "Fin" };
        for (int i = 0; i < headers1.Length; i++)
        {
            ws1.Cell(1, i + 1).Value = headers1[i];
            ws1.Cell(1, i + 1).Style.Font.Bold = true;
            ws1.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromArgb(0x2D3748);
            ws1.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
        }
        int row1 = 2;
        foreach (var s in servicios)
        {
            if (s.Horarios.Count == 0)
            {
                ws1.Cell(row1, 1).Value = s.Nombre;
                ws1.Cell(row1, 2).Value = s.CantidadPersonasRequeridas;
                ws1.Cell(row1, 3).Value = s.Activo ? "Activo" : "Inactivo";
                row1++;
            }
            else
            {
                foreach (var h in s.Horarios)
                {
                    ws1.Cell(row1, 1).Value = s.Nombre;
                    ws1.Cell(row1, 2).Value = s.CantidadPersonasRequeridas;
                    ws1.Cell(row1, 3).Value = s.Activo ? "Activo" : "Inactivo";
                    ws1.Cell(row1, 4).Value = h.Descripcion;
                    ws1.Cell(row1, 5).Value = h.FechaHoraInicio.ToString("dd/MM/yyyy HH:mm");
                    ws1.Cell(row1, 6).Value = h.FechaHoraFin.ToString("dd/MM/yyyy HH:mm");
                    row1++;
                }
            }
        }
        ws1.Columns().AdjustToContents();

        // Hoja 2: Grupos de servicio con miembros
        var ws2 = workbook.Worksheets.Add("Grupos de Servicio");
        var headers2 = new[] { "Servicio", "Horario", "Ubicación", "Grupo", "Miembro", "Edad", "Rol", "Documento", "Teléfono", "Departamento", "Ciudad", "Servicio Inscripción" };
        for (int i = 0; i < headers2.Length; i++)
        {
            ws2.Cell(1, i + 1).Value = headers2[i];
            ws2.Cell(1, i + 1).Style.Font.Bold = true;
            ws2.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromArgb(0x2D3748);
            ws2.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
        }
        int row2 = 2;
        foreach (var s in servicios)
        {
            foreach (var g in s.Grupos.OrderBy(g => g.Nombre))
            {
                // Excluir a quienes no van a asistir de la base de colaboradores
                var miembros = g.Miembros
                    .Where(m => m.Inscripcion.Estado != EstadoInscripcion.NoVaAsistir)
                    .OrderBy(m => m.UbicacionServicio?.Nombre ?? "zzz")
                    .ThenBy(m => m.HorarioServicio?.Descripcion ?? "zzz")
                    .ThenBy(m => m.Rol)
                    .ThenBy(m => m.Inscripcion.Persona.Nombres)
                    .ToList();
                if (miembros.Count == 0)
                {
                    ws2.Cell(row2, 1).Value = s.Nombre;
                    ws2.Cell(row2, 4).Value = g.Nombre;
                    row2++;
                }
                else
                {
                    foreach (var m in miembros)
                    {
                        ws2.Cell(row2, 1).Value = s.Nombre;
                        ws2.Cell(row2, 2).Value = m.HorarioServicio?.Descripcion ?? "—";
                        ws2.Cell(row2, 3).Value = m.UbicacionServicio?.Nombre ?? "—";
                        ws2.Cell(row2, 4).Value = g.Nombre;
                        ws2.Cell(row2, 5).Value = m.Inscripcion.Persona.NombreCompleto;
                        ws2.Cell(row2, 6).Value = (int)m.Inscripcion.Persona.Edad;
                        ws2.Cell(row2, 7).Value = m.Rol.NombreParaMostrar();
                        ws2.Cell(row2, 8).Value = m.Inscripcion.Persona.NumeroIdentificacion;
                        ws2.Cell(row2, 9).Value = m.Inscripcion.Persona.Telefono;
                        ws2.Cell(row2, 10).Value = m.Inscripcion.Departamento;
                        ws2.Cell(row2, 11).Value = m.Inscripcion.Ciudad;
                        ws2.Cell(row2, 12).Value = string.Join(", ", m.Inscripcion.Servicios.Select(sv => sv.Descripcion()));
                        row2++;
                    }
                }
            }
        }
        ws2.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Servicios.xlsx");
    }

    // ── Helpers ─────────────────────────────────────────────────────
    async Task<GrupoFormViewModel> BuildGrupoFormViewModel(Servicio servicio)
    {
        return new GrupoFormViewModel
        {
            ServicioId = servicio.Id,
            ServicioNombre = servicio.Nombre,
        };
    }
}

// ── ViewModels ─────────────────────────────────────────────────────

public class DashboardServiciosViewModel
{
    public List<Servicio> Servicios { get; set; } = [];
    public int TotalGrupos { get; set; }
    public int TotalMiembros { get; set; }
    public List<AlertaNoAsisteViewModel> AlertasNoAsiste { get; set; } = [];
    public List<AlertaMultipleServicioViewModel> AlertasMultiple { get; set; } = [];
}

public class AlertaNoAsisteViewModel
{
    public int MiembroId { get; set; }
    public string NombrePersona { get; set; } = "";
    public string NombreGrupo { get; set; } = "";
    public string NombreServicio { get; set; } = "";
}

public class AlertaMultipleServicioViewModel
{
    public string NombrePersona { get; set; } = "";
    public List<string> Asignaciones { get; set; } = [];
}

public class ServicioFormViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public int CantidadPersonasRequeridas { get; set; } = 1;
}

public class HorariosPageViewModel
{
    public int ServicioId { get; set; }
    public string ServicioNombre { get; set; } = "";
    public List<HorarioServicio> Horarios { get; set; } = [];
}

public class HorarioFormViewModel
{
    public int ServicioId { get; set; }
    public string ServicioNombre { get; set; } = "";
    public int? HorarioId { get; set; }
    public string Descripcion { get; set; } = "";
    public string FechaInicio { get; set; } = "";
    public string HoraInicio { get; set; } = "";
    public string FechaFin { get; set; } = "";
    public string HoraFin { get; set; } = "";
}

public class HorarioFormItem
{
    public int Id { get; set; }
    public string Descripcion { get; set; } = "";
    public string FechaInicio { get; set; } = "";
    public string HoraInicio { get; set; } = "";
    public string FechaFin { get; set; } = "";
    public string HoraFin { get; set; } = "";

    public DateTime ObtenerFechaHoraInicio() =>
        DateTime.TryParse($"{FechaInicio} {HoraInicio}", out var dt) ? DateTime.SpecifyKind(dt, DateTimeKind.Utc) : default;

    public DateTime ObtenerFechaHoraFin() =>
        DateTime.TryParse($"{FechaFin} {HoraFin}", out var dt) ? DateTime.SpecifyKind(dt, DateTimeKind.Utc) : default;
}

public class GrupoFormViewModel
{
    public int GrupoId { get; set; }
    public int ServicioId { get; set; }
    public string ServicioNombre { get; set; } = "";
    public string Nombre { get; set; } = "";
}

public class GrupoAgregarMiembrosViewModel
{
    public int GrupoId { get; set; }
    public int ServicioId { get; set; }
    public string ServicioNombre { get; set; } = "";
    public string GrupoNombre { get; set; } = "";
    public List<Inscripcion> InscripcionesDisponibles { get; set; } = [];
    public HashSet<int> IdsYaEnGrupo { get; set; } = [];
    /// <summary>InscripcionId → descripción de grupos en los que ya está asignada la persona (excluyendo este grupo)</summary>
    public Dictionary<int, string> GrupoDeOtros { get; set; } = [];
}

public class UbicacionFormViewModel
{
    public int ServicioId { get; set; }
    public string ServicioNombre { get; set; } = "";
    public int? UbicacionId { get; set; }
    public string Nombre { get; set; } = "";
}
