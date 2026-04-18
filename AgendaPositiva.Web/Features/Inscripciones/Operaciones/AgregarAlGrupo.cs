using AgendaPositiva.Web.Datos;
using AgendaPositiva.Web.Features.Admin.Regiones.Dominio;
using AgendaPositiva.Web.Features.Commons;
using AgendaPositiva.Web.Features.Inscripciones.Dominio;
using Microsoft.EntityFrameworkCore;

namespace AgendaPositiva.Web.Features.Inscripciones.Operaciones;

public record FamiliarDto(
    string Nombres,
    string Apellidos,
    Genero Genero,
    DateOnly FechaNacimiento,
    string Telefono,
    string? Email,
    TipoIdentificacion TipoIdentificacion,
    string NumeroIdentificacion,
    List<ServicioInscripcion>? Servicios,
    string? NecesidadesEspeciales,
    Parentesco? Parentesco,
    string Ciudad,
    string Departamento,
    bool TieneAlergiaAlimentaria = false,
    string? DescripcionAlergia = null,
    bool ParticipaComunionAncianos = false,
    bool RequiereAlimentacion = false,
    PreguntasAdicionalesNino? PreguntasAdicionalesNino = null,
    int? PersonaExistenteId = null
);

public class AgregarAlGrupo(AppDbContext db)
{
    readonly AppDbContext db = db;

    /// <summary>
    /// Agrega una persona existente al grupo de la inscripción principal.
    /// La relación se establece entre la persona agregada y relacionConPersonaId.
    /// </summary>
    public async Task<Result> ConPersonaExistente(int inscripcionId, int personaExistenteId, Parentesco relacion, int relacionConPersonaId)
    {
        var inscripcion = db.Inscripciones
            .Include(i => i.Persona)
            .Include(i => i.Evento)
            .FirstOrDefault(i => i.Id == inscripcionId);

        if (inscripcion is null) return Result.Failure("Inscripción no encontrada.");

        int eventoId = inscripcion.EventoId;

        var familiar = db.Inscripciones
            .FirstOrDefault(i => i.PersonaId == personaExistenteId && i.EventoId == eventoId);

        var grupoId = await ObtenerOCrearGrupo(inscripcion, familiar);

        // Asignar grupo al familiar (sin modificar su relación)
        if (familiar is not null)
        {
            familiar.GrupoFamiliarId = grupoId;
        }
        else
        {
            var evento = inscripcion.Evento;

            // Validar cupos antes de crear nueva inscripción
            if (!evento.TieneCupo)
                return Result.Failure("Se ha alcanzado el cupo total del evento. No se pueden registrar más inscripciones.");

            var regionError = await ValidarCupoRegion(eventoId, inscripcion.Departamento, inscripcion.Ciudad);
            if (regionError is not null)
                return Result.Failure(regionError);

            db.Inscripciones.Add(new Inscripcion
            {
                PersonaId = personaExistenteId,
                EventoId = eventoId,
                GrupoFamiliarId = grupoId,
                RequiereHospedaje = inscripcion.RequiereHospedaje,
                Ciudad = inscripcion.Ciudad,
                Departamento = inscripcion.Departamento,
            });

            // Incrementar contadores de cupo
            evento.TotalInscritos++;
            await IncrementarCupoRegion(eventoId, inscripcion.Departamento, inscripcion.Ciudad);
        }

        // La relación (ya invertida) se guarda en la inscripción de quien agrega
        if (inscripcion.Relacion is null)
        {
            inscripcion.Relacion = relacion;
            inscripcion.RelacionConPersonaId = relacionConPersonaId;
        }

        await db.SaveChangesAsync();
        return Result.Success();
    }

    /// <summary>
    /// Agrega una persona nueva al grupo de la inscripción principal.
    /// </summary>
    public async Task<Result> ConPersonaNueva(int inscripcionId, FamiliarDto datos, Parentesco relacion, int relacionConPersonaId)
    {
        var inscripcion = db.Inscripciones
            .Include(i => i.Persona)
            .Include(i => i.Evento)
            .FirstOrDefault(i => i.Id == inscripcionId);

        if (inscripcion is null) return Result.Failure("Inscripción no encontrada.");

        int eventoId = inscripcion.EventoId;

        var evento = inscripcion.Evento;

        // Validar cupos antes de crear nueva inscripción
        if (!evento.TieneCupo)
            return Result.Failure("Se ha alcanzado el cupo total del evento. No se pueden registrar más inscripciones.");

        var regionError = await ValidarCupoRegion(eventoId, datos.Departamento, datos.Ciudad);
        if (regionError is not null)
            return Result.Failure(regionError);

        var grupoId = await ObtenerOCrearGrupo(inscripcion, inscFamiliar: null);

        var persona = new Persona
        {
            Nombres = datos.Nombres,
            Apellidos = datos.Apellidos,
            Genero = datos.Genero,
            FechaNacimiento = datos.FechaNacimiento,
            Telefono = datos.Telefono,
            Email = datos.Email,
            TipoIdentificacion = datos.TipoIdentificacion,
            NumeroIdentificacion = datos.NumeroIdentificacion,
        };
        db.Personas.Add(persona);

        db.Inscripciones.Add(new Inscripcion
        {
            Persona = persona,
            EventoId = eventoId,
            GrupoFamiliarId = grupoId,
            RequiereHospedaje = inscripcion.RequiereHospedaje,
            Servicios = datos.Servicios ?? [],
            NecesidadesEspeciales = datos.NecesidadesEspeciales,
            Ciudad = datos.Ciudad,
            Departamento = datos.Departamento,
            TieneAlergiaAlimentaria = datos.TieneAlergiaAlimentaria,
            DescripcionAlergia = datos.DescripcionAlergia,
            ParticipaComunionAncianos = datos.ParticipaComunionAncianos,
            RequiereAlimentacion = datos.RequiereAlimentacion,
            PreguntasAdicionalesNino = datos.PreguntasAdicionalesNino,
            Relacion = relacion,
            RelacionConPersonaId = relacionConPersonaId,
        });

        // Incrementar contadores de cupo
        evento.TotalInscritos++;
        await IncrementarCupoRegion(eventoId, datos.Departamento, datos.Ciudad);

        await db.SaveChangesAsync();
        return Result.Success();
    }

    /// <summary>
    /// Obtiene o crea el grupo familiar para la inscripción principal.
    /// </summary>
    async Task<int> ObtenerOCrearGrupo(Inscripcion inscripcionPrincipal, Inscripcion? inscFamiliar)
    {
        if (inscripcionPrincipal.GrupoFamiliarId is not null)
            return inscripcionPrincipal.GrupoFamiliarId.Value;

        if (inscFamiliar?.GrupoFamiliarId is not null)
        {
            inscripcionPrincipal.GrupoFamiliarId = inscFamiliar.GrupoFamiliarId.Value;
            await db.SaveChangesAsync();
            return inscFamiliar.GrupoFamiliarId.Value;
        }

        var grupo = new GrupoFamiliar();
        db.GrupoFamiliar.Add(grupo);
        await db.SaveChangesAsync();

        inscripcionPrincipal.GrupoFamiliarId = grupo.Id;
        await db.SaveChangesAsync();

        return grupo.Id;
    }

    async Task<string?> ValidarCupoRegion(int eventoId, string departamento, string ciudad)
    {
        var regiones = await db.Set<RegionEvento>()
            .Where(r => r.EventoId == eventoId)
            .ToListAsync();

        var region = regiones.FirstOrDefault(r => r.Contiene(departamento, ciudad));
        if (region is not null && !region.TieneCupo)
            return $"Se ha alcanzado el cupo de la región «{region.Nombre}». No se pueden registrar más inscripciones para esta zona.";

        return null;
    }

    async Task IncrementarCupoRegion(int eventoId, string departamento, string ciudad)
    {
        var regiones = await db.Set<RegionEvento>()
            .Where(r => r.EventoId == eventoId)
            .ToListAsync();

        var region = regiones.FirstOrDefault(r => r.Contiene(departamento, ciudad));
        if (region is not null)
            region.TotalInscritos++;
    }
}
