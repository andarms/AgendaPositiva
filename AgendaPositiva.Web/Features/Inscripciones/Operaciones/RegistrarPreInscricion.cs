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
    Parentesco? Parentesco,
    string Ciudad,
    string Departamento,
    int? PersonaExistenteId = null
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
    string Ciudad,
    string Departamento,
    bool ViajaConEsposa,
    FamiliarCommand? Esposa,
    bool ViajaConHijos,
    List<FamiliarCommand>? Hijos,
    bool ViajaConOtrosFamiliares,
    List<FamiliarCommand>? OtrosFamiliares
);

public class RegistrarPreInscricion
{
    readonly AppDbContext db;

    public RegistrarPreInscricion(AppDbContext db)
    {
        this.db = db;
    }

    public async Task<Result<int>> Handle(RegistrarPreInscricionCommand command)
    {
        Evento? evento = db.Eventos.FirstOrDefault(e => e.Activo);
        if (evento is null)
            return Result.Failure<int>("No hay un evento activo.");

        // 1. Persona principal
        var personaPrincipal = CrearPersona(command);
        db.Personas.Add(personaPrincipal);
        await db.SaveChangesAsync();

        // 2. Buscar si algún familiar existente ya tiene grupo en este evento
        int? grupoExistenteId = BuscarGrupoExistente(command, evento.Id);
        bool uniendoseAGrupoExistente = grupoExistenteId is not null;

        int grupoId;
        if (uniendoseAGrupoExistente)
        {
            // Unirse al grupo existente (el líder sigue siendo el mismo)
            grupoId = grupoExistenteId!.Value;
        }
        else
        {
            // Crear nuevo grupo con la persona principal como líder
            var grupo = new GrupoFamiliar { LiderGrupoId = personaPrincipal.Id };
            db.GrupoFamiliar.Add(grupo);
            await db.SaveChangesAsync();
            grupoId = grupo.Id;
        }

        // 3. Inscripción de la persona principal (sin relación — es líder o se asigna después)
        var inscripcionLider = new Inscripcion
        {
            PersonaId = personaPrincipal.Id,
            EventoId = evento.Id,
            GrupoFamiliarId = grupoId,
            RequiereHospedaje = command.RequiereHospedaje,
            NecesidadesEspeciales = command.NecesidadesEspeciales,
            Ciudad = command.Ciudad,
            Departamento = command.Departamento,
        };
        db.Inscripciones.Add(inscripcionLider);

        // 4. Esposa/o
        if (command.ViajaConEsposa && command.Esposa is not null)
        {
            var relacion = command.Genero == Genero.Masculino
                ? Parentesco.Esposa
                : Parentesco.Esposo;

            RegistrarFamiliar(command.Esposa, evento.Id, grupoId, command.RequiereHospedaje, relacion, personaPrincipal.Id);
        }

        // 5. Hijos (0+n)
        if (command.ViajaConHijos && command.Hijos is { Count: > 0 })
        {
            foreach (var hijo in command.Hijos)
            {
                var relacion = hijo.Genero == Genero.Masculino
                    ? Parentesco.Hijo
                    : Parentesco.Hija;

                RegistrarFamiliar(hijo, evento.Id, grupoId, command.RequiereHospedaje, relacion, personaPrincipal.Id);
            }
        }

        // 6. Otros familiares (0+n)
        if (command.ViajaConOtrosFamiliares && command.OtrosFamiliares is { Count: > 0 })
        {
            foreach (var familiar in command.OtrosFamiliares)
            {
                RegistrarFamiliar(familiar, evento.Id, grupoId, command.RequiereHospedaje,
                    familiar.Parentesco ?? Parentesco.Otro, personaPrincipal.Id);
            }
        }

        await db.SaveChangesAsync();

        return Result.Success(inscripcionLider.Id);
    }

    int? BuscarGrupoExistente(RegistrarPreInscricionCommand cmd, int eventoId)
    {
        var ids = new List<int>();
        if (cmd.ViajaConEsposa && cmd.Esposa?.PersonaExistenteId is int esposaId)
            ids.Add(esposaId);
        if (cmd.ViajaConHijos && cmd.Hijos is not null)
            ids.AddRange(cmd.Hijos.Where(h => h.PersonaExistenteId.HasValue).Select(h => h.PersonaExistenteId!.Value));
        if (cmd.ViajaConOtrosFamiliares && cmd.OtrosFamiliares is not null)
            ids.AddRange(cmd.OtrosFamiliares.Where(f => f.PersonaExistenteId.HasValue).Select(f => f.PersonaExistenteId!.Value));

        if (ids.Count == 0) return null;

        return db.Inscripciones
            .Where(i => ids.Contains(i.PersonaId) && i.EventoId == eventoId && i.GrupoFamiliarId != null)
            .Select(i => i.GrupoFamiliarId)
            .FirstOrDefault();
    }

    void RegistrarFamiliar(FamiliarCommand datos, int eventoId, int grupoId, bool requiereHospedaje, Parentesco relacion, int relacionConPersonaId)
    {
        if (datos.PersonaExistenteId is int personaId)
        {
            var inscripcionExistente = db.Inscripciones
                .FirstOrDefault(i => i.PersonaId == personaId && i.EventoId == eventoId);

            if (inscripcionExistente is not null)
            {
                inscripcionExistente.GrupoFamiliarId = grupoId;
                if (inscripcionExistente.Relacion is null)
                {
                    inscripcionExistente.Relacion = relacion;
                    inscripcionExistente.RelacionConPersonaId = relacionConPersonaId;
                }
            }
            else
            {
                db.Inscripciones.Add(new Inscripcion
                {
                    PersonaId = personaId,
                    EventoId = eventoId,
                    GrupoFamiliarId = grupoId,
                    RequiereHospedaje = requiereHospedaje,
                    NecesidadesEspeciales = datos.NecesidadesEspeciales,
                    Ciudad = datos.Ciudad,
                    Departamento = datos.Departamento,
                    Relacion = relacion,
                    RelacionConPersonaId = relacionConPersonaId,
                });
            }
        }
        else
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
            db.Personas.Add(persona);

            db.Inscripciones.Add(new Inscripcion
            {
                Persona = persona,
                EventoId = eventoId,
                GrupoFamiliarId = grupoId,
                RequiereHospedaje = requiereHospedaje,
                NecesidadesEspeciales = datos.NecesidadesEspeciales,
                Ciudad = datos.Ciudad,
                Departamento = datos.Departamento,
                Relacion = relacion,
                RelacionConPersonaId = relacionConPersonaId,
            });
        }
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
}