using AgendaPositiva.Web.Datos;
using AgendaPositiva.Web.Features.Commons;
using AgendaPositiva.Web.Features.Inscripciones.Dominio;

namespace AgendaPositiva.Web.Features.Inscripciones.Operaciones;

public class AgregarAlGrupo
{
    readonly AppDbContext db;

    public AgregarAlGrupo(AppDbContext db)
    {
        this.db = db;
    }

    public async Task<Result> ConPersonaExistente(int inscripcionId, int personaExistenteId, RelacionConLider relacion)
    {
        var inscripcion = await db.Inscripciones.FindAsync(inscripcionId);
        if (inscripcion is null)
            return Result.Failure("Inscripción no encontrada.");

        int grupoId = inscripcion.GrupoAsistenciaId!.Value;
        int eventoId = inscripcion.EventoId;

        var inscExistente = db.Inscripciones
            .FirstOrDefault(i => i.PersonaId == personaExistenteId && i.EventoId == eventoId);

        if (inscExistente is not null)
        {
            inscExistente.GrupoAsistenciaId = grupoId;
            inscExistente.RelacionConLider = relacion;
        }
        else
        {
            db.Inscripciones.Add(new Inscripcion
            {
                PersonaId = personaExistenteId,
                EventoId = eventoId,
                GrupoAsistenciaId = grupoId,
                RequiereHospedaje = inscripcion.RequiereHospedaje,
                RelacionConLider = relacion,
            });
        }

        await db.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> ConPersonaNueva(int inscripcionId, FamiliarCommand datos, RelacionConLider relacion)
    {
        var inscripcion = await db.Inscripciones.FindAsync(inscripcionId);
        if (inscripcion is null)
            return Result.Failure("Inscripción no encontrada.");

        int grupoId = inscripcion.GrupoAsistenciaId!.Value;
        int eventoId = inscripcion.EventoId;

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
            GrupoAsistenciaId = grupoId,
            RequiereHospedaje = inscripcion.RequiereHospedaje,
            NecesidadesEspeciales = datos.NecesidadesEspeciales,
            RelacionConLider = relacion,
        });

        await db.SaveChangesAsync();
        return Result.Success();
    }
}
