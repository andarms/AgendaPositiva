using AgendaPositiva.Web.Datos;
using AgendaPositiva.Web.Features.Commons;
using AgendaPositiva.Web.Features.Inscripciones.Dominio;

namespace AgendaPositiva.Web.Features.Inscripciones.Operaciones;

public record FamiliarCommand(
    string Nombres,
    string Apellidos,
    Genero Genero,
    DateOnly FechaNacimiento,
    string Telefono,
    string? Email,
    TipoIdentificacion TipoIdentificacion,
    string NumeroIdentificacion,
    string? NecesidadesEspeciales,
    RelacionConLider? RelacionConLider
);

public record RegistrarPreInscricionCommand(
    string Nombres,
    string Apellidos,
    Genero Genero,
    DateOnly FechaNacimiento,
    string Telefono,
    string? Email,
    TipoIdentificacion TipoIdentificacion,
    string NumeroIdentificacion,
    bool RequiereHospedaje,
    string? NecesidadesEspeciales,
    bool ViajaConEsposa,
    FamiliarCommand? Esposa,
    bool ViajaConHijos,
    List<FamiliarCommand>? Hijos,
    bool ViajaConOtrosFamiliares,
    List<FamiliarCommand>? OtrosFamiliares
);

public class RegistrarPreInscricion
{
    readonly AppDbContext _db;

    public RegistrarPreInscricion(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Result<int>> Handle(RegistrarPreInscricionCommand command)
    {
        Evento? evento = _db.Eventos.FirstOrDefault(e => e.Activo);
        if (evento is null)
            return Result.Failure<int>("No hay un evento activo.");

        // 1. Persona principal
        var personaPrincipal = CrearPersona(command);
        _db.Personas.Add(personaPrincipal);
        await _db.SaveChangesAsync();

        // 2. Grupo familiar con líder
        var grupo = new GrupoFamiliar { LiderGrupoId = personaPrincipal.Id };
        _db.GrupoFamiliar.Add(grupo);
        await _db.SaveChangesAsync();

        // 3. Inscripción del líder (sin RelacionConLider)
        var inscripcionLider = CrearInscripcion(
            personaPrincipal.Id, evento.Id, grupo.Id,
            command.RequiereHospedaje, command.NecesidadesEspeciales,
            relacionConLider: null
        );
        _db.Inscripciones.Add(inscripcionLider);

        // 4. Esposa/o
        if (command.ViajaConEsposa && command.Esposa is not null)
        {
            var relacion = command.Genero == Genero.Masculino
                ? Commons.RelacionConLider.Esposa
                : Commons.RelacionConLider.Esposo;

            RegistrarFamiliar(command.Esposa, evento.Id, grupo.Id, command.RequiereHospedaje, relacion);
        }

        // 5. Hijos (0+n)
        if (command.ViajaConHijos && command.Hijos is { Count: > 0 })
        {
            foreach (var hijo in command.Hijos)
            {
                var relacion = hijo.Genero == Genero.Masculino
                    ? Commons.RelacionConLider.Hijo
                    : Commons.RelacionConLider.Hija;

                RegistrarFamiliar(hijo, evento.Id, grupo.Id, command.RequiereHospedaje, relacion);
            }
        }

        // 6. Otros familiares (0+n)
        if (command.ViajaConOtrosFamiliares && command.OtrosFamiliares is { Count: > 0 })
        {
            foreach (var familiar in command.OtrosFamiliares)
            {
                RegistrarFamiliar(familiar, evento.Id, grupo.Id, command.RequiereHospedaje,
                    familiar.RelacionConLider ?? Commons.RelacionConLider.Otro);
            }
        }

        await _db.SaveChangesAsync();

        return Result.Success(inscripcionLider.Id);
    }

    void RegistrarFamiliar(FamiliarCommand datos, int eventoId, int grupoId, bool requiereHospedaje, RelacionConLider relacion)
    {
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
        _db.Personas.Add(persona);

        var inscripcion = CrearInscripcion(persona, eventoId, grupoId, requiereHospedaje, datos.NecesidadesEspeciales, relacion);
        _db.Inscripciones.Add(inscripcion);
    }

    static Persona CrearPersona(RegistrarPreInscricionCommand cmd) => new()
    {
        Nombres = cmd.Nombres,
        Apellidos = cmd.Apellidos,
        Genero = cmd.Genero,
        FechaNacimiento = cmd.FechaNacimiento,
        Telefono = cmd.Telefono,
        Email = cmd.Email,
        TipoIdentificacion = cmd.TipoIdentificacion,
        NumeroIdentificacion = cmd.NumeroIdentificacion,
    };

    static Inscripcion CrearInscripcion(int personaId, int eventoId, int grupoId, bool requiereHospedaje, string? necesidades, RelacionConLider? relacionConLider) => new()
    {
        PersonaId = personaId,
        EventoId = eventoId,
        GrupoAsistenciaId = grupoId,
        RequiereHospedaje = requiereHospedaje,
        NecesidadesEspeciales = necesidades,
        RelacionConLider = relacionConLider,
    };

    static Inscripcion CrearInscripcion(Persona persona, int eventoId, int grupoId, bool requiereHospedaje, string? necesidades, RelacionConLider? relacionConLider) => new()
    {
        Persona = persona,
        EventoId = eventoId,
        GrupoAsistenciaId = grupoId,
        RequiereHospedaje = requiereHospedaje,
        NecesidadesEspeciales = necesidades,
        RelacionConLider = relacionConLider,
    };
}