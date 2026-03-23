using AgendaPositiva.Web.Datos;
using AgendaPositiva.Web.Features.Commons;
using AgendaPositiva.Web.Features.Inscripciones.Dominio;

namespace AgendaPositiva.Web.Features.Inscripciones.Operaciones;

public record RegistrarPersonaPrincipalCommand(
    string Nombres,
    string Apellidos,
    Genero Genero,
    DateOnly FechaNacimiento,
    string Telefono,
    string? Email,
    TipoIdentificacion TipoIdentificacion,
    string NumeroIdentificacion,
    bool RequiereHospedaje,
    string? NecesidadesEspeciales
);

public class RegistrarPersonaPrincipal
{
    readonly AppDbContext db;

    public RegistrarPersonaPrincipal(AppDbContext db)
    {
        this.db = db;
    }

    public async Task<Result<int>> Handle(RegistrarPersonaPrincipalCommand command)
    {
        Evento? evento = db.Eventos.FirstOrDefault(e => e.Activo);
        if (evento is null)
            return Result.Failure<int>("No hay un evento activo.");

        var persona = new Persona
        {
            Nombres = command.Nombres,
            Apellidos = command.Apellidos,
            Genero = command.Genero,
            FechaNacimiento = command.FechaNacimiento,
            Telefono = command.Telefono,
            Email = command.Email,
            TipoIdentificacion = command.TipoIdentificacion,
            NumeroIdentificacion = command.NumeroIdentificacion,
        };
        db.Personas.Add(persona);
        await db.SaveChangesAsync();

        var grupo = new GrupoFamiliar { LiderGrupoId = persona.Id };
        db.GrupoFamiliar.Add(grupo);
        await db.SaveChangesAsync();

        var inscripcion = new Inscripcion
        {
            PersonaId = persona.Id,
            EventoId = evento.Id,
            GrupoAsistenciaId = grupo.Id,
            RequiereHospedaje = command.RequiereHospedaje,
            NecesidadesEspeciales = command.NecesidadesEspeciales,
        };
        db.Inscripciones.Add(inscripcion);
        await db.SaveChangesAsync();

        return Result.Success(inscripcion.Id);
    }
}
