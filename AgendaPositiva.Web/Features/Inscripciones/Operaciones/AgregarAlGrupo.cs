using AgendaPositiva.Web.Datos;
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
    string? NecesidadesEspeciales,
    Parentesco? Parentesco,
    string Ciudad,
    string Departamento,
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
    public async Task<Result> ConPersonaNueva(int inscripcionId, FamiliarDto datos, Parentesco relacion, int relacionConPersonaId)
    {
        var inscripcion = db.Inscripciones
            .Include(i => i.Persona)
            .FirstOrDefault(i => i.Id == inscripcionId);

        if (inscripcion is null) return Result.Failure("Inscripción no encontrada.");

        int eventoId = inscripcion.EventoId;

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
}
