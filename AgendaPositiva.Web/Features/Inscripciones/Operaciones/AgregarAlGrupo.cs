using AgendaPositiva.Web.Datos;
using AgendaPositiva.Web.Features.Commons;
using AgendaPositiva.Web.Features.Inscripciones.Dominio;
using Microsoft.EntityFrameworkCore;

namespace AgendaPositiva.Web.Features.Inscripciones.Operaciones;

public class AgregarAlGrupo
{
    readonly AppDbContext db;

    public AgregarAlGrupo(AppDbContext db)
    {
        this.db = db;
    }

    /// <summary>
    /// Agrega una persona existente al grupo de la inscripción principal.
    /// La relación se establece entre la persona agregada y relacionConPersonaId.
    /// </summary>
    public async Task<Result> ConPersonaExistente(int inscripcionId, int personaExistenteId, Parentesco relacion, int relacionConPersonaId)
    {
        var inscripcion = db.Inscripciones
            .Include(i => i.Persona)
            .FirstOrDefault(i => i.Id == inscripcionId);
        if (inscripcion is null)
            return Result.Failure("Inscripción no encontrada.");

        int eventoId = inscripcion.EventoId;

        var inscFamiliar = db.Inscripciones
            .FirstOrDefault(i => i.PersonaId == personaExistenteId && i.EventoId == eventoId);

        var (grupoId, _) = await ObtenerOCrearGrupo(inscripcion, inscFamiliar);

        // Asignar grupo al familiar (sin modificar su relación)
        if (inscFamiliar is not null)
        {
            inscFamiliar.GrupoFamiliarId = grupoId;
        }
        else
        {
            db.Inscripciones.Add(new Inscripcion
            {
                PersonaId = personaExistenteId,
                EventoId = eventoId,
                GrupoFamiliarId = grupoId,
                RequiereHospedaje = inscripcion.RequiereHospedaje,
                Ciudad = inscripcion.Ciudad,
                Departamento = inscripcion.Departamento,
            });
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
    public async Task<Result> ConPersonaNueva(int inscripcionId, FamiliarCommand datos, Parentesco relacion, int relacionConPersonaId)
    {
        var inscripcion = db.Inscripciones
            .Include(i => i.Persona)
            .FirstOrDefault(i => i.Id == inscripcionId);
        if (inscripcion is null)
            return Result.Failure("Inscripción no encontrada.");

        int eventoId = inscripcion.EventoId;

        var (grupoId, _) = await ObtenerOCrearGrupo(inscripcion, inscFamiliar: null);

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
            NecesidadesEspeciales = datos.NecesidadesEspeciales,
            Ciudad = datos.Ciudad,
            Departamento = datos.Departamento,
            Relacion = relacion,
            RelacionConPersonaId = relacionConPersonaId,
        });

        await db.SaveChangesAsync();
        return Result.Success();
    }

    /// <summary>
    /// Obtiene o crea el grupo familiar para la inscripción principal.
    /// Retorna (grupoId, principalEsLider).
    /// </summary>
    async Task<(int GrupoId, bool PrincipalEsLider)> ObtenerOCrearGrupo(Inscripcion inscripcionPrincipal, Inscripcion? inscFamiliar)
    {
        // 1. La persona principal ya tiene grupo
        if (inscripcionPrincipal.GrupoFamiliarId is not null)
            return (inscripcionPrincipal.GrupoFamiliarId.Value, true);

        // 2. El familiar ya tiene grupo → unirse a ese grupo (principal NO es líder)
        if (inscFamiliar?.GrupoFamiliarId is not null)
        {
            inscripcionPrincipal.GrupoFamiliarId = inscFamiliar.GrupoFamiliarId.Value;
            await db.SaveChangesAsync();
            return (inscFamiliar.GrupoFamiliarId.Value, false);
        }

        // 3. Nadie tiene grupo → crear uno nuevo, persona principal es líder
        var grupo = new GrupoFamiliar { LiderGrupoId = inscripcionPrincipal.PersonaId };
        db.GrupoFamiliar.Add(grupo);
        await db.SaveChangesAsync();

        inscripcionPrincipal.GrupoFamiliarId = grupo.Id;
        await db.SaveChangesAsync();

        return (grupo.Id, true);
    }
}
