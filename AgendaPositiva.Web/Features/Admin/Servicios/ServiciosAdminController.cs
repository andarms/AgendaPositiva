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
[Authorize(Roles = "Administrador")]
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
            .Include(s => s.Grupos).ThenInclude(g => g.HorarioServicio)
            .Include(s => s.Grupos).ThenInclude(g => g.Miembros).ThenInclude(m => m.Inscripcion).ThenInclude(i => i.Persona)
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

        var tieneGrupos = await store.GruposServicio.AnyAsync(g => g.HorarioServicioId == id);
        if (tieneGrupos)
        {
            TempData["Error"] = "No se puede eliminar el horario porque tiene grupos asignados. Elimine los grupos primero.";
            return RedirectToAction(nameof(HorariosIndex), new { servicioId });
        }

        store.HorariosServicio.Remove(horario);
        await store.SaveChangesAsync();
        TempData["Mensaje"] = "Horario eliminado.";
        return RedirectToAction(nameof(HorariosIndex), new { servicioId });
    }

    // ── Grupos: listado ────────────────────────────────────────────
    [HttpGet("{servicioId:int}/grupos")]
    public async Task<IActionResult> GruposIndex(int servicioId)
    {
        var servicio = await store.Servicios
            .Include(s => s.Horarios.OrderBy(h => h.FechaHoraInicio))
            .Include(s => s.Grupos).ThenInclude(g => g.HorarioServicio)
            .Include(s => s.Grupos).ThenInclude(g => g.Miembros).ThenInclude(m => m.Inscripcion).ThenInclude(i => i.Persona)
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
    public async Task<IActionResult> GrupoCrearPost(int servicioId, string nombre, int horarioServicioId, int? liderInscripcionId, List<int> miembrosIds)
    {
        var servicio = await store.Servicios
            .Include(s => s.Horarios)
            .FirstOrDefaultAsync(s => s.Id == servicioId);
        if (servicio is null) return NotFound();

        if (string.IsNullOrWhiteSpace(nombre) || horarioServicioId == 0)
        {
            ModelState.AddModelError("", "Nombre y horario son requeridos.");
            var vm = await BuildGrupoFormViewModel(servicio);
            vm.Nombre = nombre;
            return View("~/Features/Admin/Servicios/Views/GrupoFormulario.cshtml", vm);
        }

        var grupo = new GrupoServicio
        {
            Nombre = nombre.Trim(),
            ServicioId = servicioId,
            HorarioServicioId = horarioServicioId,
            LiderInscripcionId = liderInscripcionId
        };

        foreach (var mid in miembrosIds?.Distinct() ?? [])
        {
            grupo.Miembros.Add(new MiembroGrupoServicio { InscripcionId = mid });
        }

        store.GruposServicio.Add(grupo);
        await store.SaveChangesAsync();
        return RedirectToAction(nameof(GruposIndex), new { servicioId });
    }

    // ── Grupos: editar ─────────────────────────────────────────────
    [HttpGet("grupos/{id:int}/editar")]
    public async Task<IActionResult> GrupoEditar(int id)
    {
        var grupo = await store.GruposServicio
            .Include(g => g.Servicio).ThenInclude(s => s.Horarios.OrderBy(h => h.FechaHoraInicio))
            .Include(g => g.Miembros)
            .FirstOrDefaultAsync(g => g.Id == id);
        if (grupo is null) return NotFound();

        var vm = await BuildGrupoFormViewModel(grupo.Servicio);
        vm.GrupoId = grupo.Id;
        vm.Nombre = grupo.Nombre;
        vm.HorarioServicioId = grupo.HorarioServicioId;
        vm.LiderInscripcionId = grupo.LiderInscripcionId;
        vm.MiembrosSeleccionadosIds = grupo.Miembros.Select(m => m.InscripcionId).ToList();
        return View("~/Features/Admin/Servicios/Views/GrupoFormulario.cshtml", vm);
    }

    [HttpPost("grupos/{id:int}/editar")]
    public async Task<IActionResult> GrupoEditarPost(int id, string nombre, int horarioServicioId, int? liderInscripcionId, List<int> miembrosIds)
    {
        var grupo = await store.GruposServicio
            .Include(g => g.Miembros)
            .Include(g => g.Servicio).ThenInclude(s => s.Horarios)
            .FirstOrDefaultAsync(g => g.Id == id);
        if (grupo is null) return NotFound();

        if (string.IsNullOrWhiteSpace(nombre) || horarioServicioId == 0)
        {
            ModelState.AddModelError("", "Nombre y horario son requeridos.");
            var vm = await BuildGrupoFormViewModel(grupo.Servicio);
            vm.GrupoId = id;
            vm.Nombre = nombre;
            return View("~/Features/Admin/Servicios/Views/GrupoFormulario.cshtml", vm);
        }

        grupo.Nombre = nombre.Trim();
        grupo.HorarioServicioId = horarioServicioId;
        grupo.LiderInscripcionId = liderInscripcionId;

        // Sync miembros
        var nuevosIds = (miembrosIds ?? []).Distinct().ToHashSet();
        var existentes = grupo.Miembros.ToList();

        foreach (var m in existentes.Where(m => !nuevosIds.Contains(m.InscripcionId)))
            store.MiembrosGrupoServicio.Remove(m);

        var idsExistentes = existentes.Select(m => m.InscripcionId).ToHashSet();
        foreach (var mid in nuevosIds.Where(mid => !idsExistentes.Contains(mid)))
            grupo.Miembros.Add(new MiembroGrupoServicio { InscripcionId = mid });

        await store.SaveChangesAsync();
        return RedirectToAction(nameof(GruposIndex), new { servicioId = grupo.ServicioId });
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

    // ── Remover miembro (desde dashboard) ──────────────────────────
    [HttpPost("miembros/{id:int}/remover")]
    public async Task<IActionResult> RemoverMiembro(int id)
    {
        var miembro = await store.MiembrosGrupoServicio.FindAsync(id);
        if (miembro is null) return NotFound();

        store.MiembrosGrupoServicio.Remove(miembro);
        await store.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // ── Excel export ───────────────────────────────────────────────
    [HttpGet("exportar")]
    public async Task<IActionResult> Exportar()
    {
        var servicios = await store.Servicios
            .Include(s => s.Horarios.OrderBy(h => h.FechaHoraInicio))
            .Include(s => s.Grupos).ThenInclude(g => g.HorarioServicio)
            .Include(s => s.Grupos).ThenInclude(g => g.Miembros).ThenInclude(m => m.Inscripcion).ThenInclude(i => i.Persona)
            .Include(s => s.Grupos).ThenInclude(g => g.LiderInscripcion).ThenInclude(i => i!.Persona)
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
        var headers2 = new[] { "Servicio", "Grupo", "Horario", "Líder", "Miembro", "Documento", "Departamento", "Ciudad", "Servicio Inscripción" };
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
                var lider = g.LiderInscripcion?.Persona?.NombreCompleto ?? "—";
                if (g.Miembros.Count == 0)
                {
                    ws2.Cell(row2, 1).Value = s.Nombre;
                    ws2.Cell(row2, 2).Value = g.Nombre;
                    ws2.Cell(row2, 3).Value = g.HorarioServicio?.Descripcion ?? "";
                    ws2.Cell(row2, 4).Value = lider;
                    row2++;
                }
                else
                {
                    foreach (var m in g.Miembros)
                    {
                        ws2.Cell(row2, 1).Value = s.Nombre;
                        ws2.Cell(row2, 2).Value = g.Nombre;
                        ws2.Cell(row2, 3).Value = g.HorarioServicio?.Descripcion ?? "";
                        ws2.Cell(row2, 4).Value = lider;
                        ws2.Cell(row2, 5).Value = m.Inscripcion.Persona.NombreCompleto;
                        ws2.Cell(row2, 6).Value = m.Inscripcion.Persona.NumeroIdentificacion;
                        ws2.Cell(row2, 7).Value = m.Inscripcion.Departamento;
                        ws2.Cell(row2, 8).Value = m.Inscripcion.Ciudad;
                        ws2.Cell(row2, 9).Value = string.Join(", ", m.Inscripcion.Servicios.Select(sv => sv.Descripcion()));
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
        var evento = await store.Eventos.FirstOrDefaultAsync(e => e.Activo);
        var inscripciones = evento is null
            ? []
            : await store.Inscripciones
                .Include(i => i.Persona)
                .Where(i => i.EventoId == evento.Id && i.Estado != EstadoInscripcion.NoVaAsistir)
                .OrderBy(i => i.Persona.Nombres).ThenBy(i => i.Persona.Apellidos)
                .ToListAsync();

        return new GrupoFormViewModel
        {
            ServicioId = servicio.Id,
            ServicioNombre = servicio.Nombre,
            Horarios = servicio.Horarios.ToList(),
            InscripcionesDisponibles = inscripciones
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
    public int HorarioServicioId { get; set; }
    public int? LiderInscripcionId { get; set; }
    public List<int> MiembrosSeleccionadosIds { get; set; } = [];
    public List<HorarioServicio> Horarios { get; set; } = [];
    public List<Inscripcion> InscripcionesDisponibles { get; set; } = [];
}
