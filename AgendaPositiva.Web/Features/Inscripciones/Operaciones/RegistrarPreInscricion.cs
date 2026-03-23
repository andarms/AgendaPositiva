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
    RelacionConLider? RelacionConLider,
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

        // 3. Inscripción de la persona principal
        //    Si se une a un grupo existente, necesita una relación con el líder;
        //    si es líder de un grupo nuevo, no tiene relación.
        RelacionConLider? relacionPrincipal = uniendoseAGrupoExistente
            ? DeterminarRelacionConLiderExistente(command)
            : null;

        var inscripcionLider = CrearInscripcion(
            personaPrincipal.Id, evento.Id, grupoId,
            command.RequiereHospedaje, command.NecesidadesEspeciales,
            relacionConLider: relacionPrincipal
        );
        db.Inscripciones.Add(inscripcionLider);

        // 4. Esposa/o
        if (command.ViajaConEsposa && command.Esposa is not null)
        {
            var relacion = command.Genero == Genero.Masculino
                ? RelacionConLider.Esposa
                : RelacionConLider.Esposo;

            RegistrarFamiliar(command.Esposa, evento.Id, grupoId, command.RequiereHospedaje, relacion, uniendoseAGrupoExistente);
        }

        // 5. Hijos (0+n)
        if (command.ViajaConHijos && command.Hijos is { Count: > 0 })
        {
            foreach (var hijo in command.Hijos)
            {
                var relacion = hijo.Genero == Genero.Masculino
                    ? RelacionConLider.Hijo
                    : RelacionConLider.Hija;

                RegistrarFamiliar(hijo, evento.Id, grupoId, command.RequiereHospedaje, relacion, uniendoseAGrupoExistente);
            }
        }

        // 6. Otros familiares (0+n)
        if (command.ViajaConOtrosFamiliares && command.OtrosFamiliares is { Count: > 0 })
        {
            foreach (var familiar in command.OtrosFamiliares)
            {
                RegistrarFamiliar(familiar, evento.Id, grupoId, command.RequiereHospedaje,
                    familiar.RelacionConLider ?? RelacionConLider.Otro, uniendoseAGrupoExistente);
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
            .Where(i => ids.Contains(i.PersonaId) && i.EventoId == eventoId && i.GrupoAsistenciaId != null)
            .Select(i => i.GrupoAsistenciaId)
            .FirstOrDefault();
    }

    /// <summary>
    /// Cuando la persona principal se une a un grupo existente,
    /// determina su relación con el líder de ese grupo usando el inverso
    /// de la relación declarada y el género de la persona principal.
    /// Ejemplo: A (Masculino) dice "B es mi Esposa" → A es Esposo de B (el líder).
    /// </summary>
    RelacionConLider? DeterminarRelacionConLiderExistente(RegistrarPreInscricionCommand cmd)
    {
        // Encontrar la primera relación declarada con una persona existente
        RelacionConLider? relacionDeclarada = null;

        if (cmd.ViajaConEsposa && cmd.Esposa?.PersonaExistenteId is not null)
        {
            relacionDeclarada = cmd.Genero == Genero.Masculino
                ? RelacionConLider.Esposa
                : RelacionConLider.Esposo;
        }
        else if (cmd.ViajaConHijos && cmd.Hijos is not null)
        {
            var hijoExistente = cmd.Hijos.FirstOrDefault(h => h.PersonaExistenteId.HasValue);
            if (hijoExistente is not null)
                relacionDeclarada = hijoExistente.Genero == Genero.Masculino
                    ? RelacionConLider.Hijo
                    : RelacionConLider.Hija;
        }
        else if (cmd.ViajaConOtrosFamiliares && cmd.OtrosFamiliares is not null)
        {
            var familiarExistente = cmd.OtrosFamiliares.FirstOrDefault(f => f.PersonaExistenteId.HasValue);
            if (familiarExistente is not null)
                relacionDeclarada = familiarExistente.RelacionConLider;
        }

        if (relacionDeclarada is null) return null;

        return InvertirRelacion(relacionDeclarada.Value, cmd.Genero);
    }

    /// <summary>
    /// Dado "A dice que B es su [relacion]", retorna qué es A para B,
    /// usando el género de A para elegir la variante masculina/femenina.
    /// </summary>
    static RelacionConLider InvertirRelacion(RelacionConLider relacion, Genero generoDelNuevo) => relacion switch
    {
        // Esposa/o ↔ Esposo/a
        RelacionConLider.Esposa or RelacionConLider.Esposo
            => generoDelNuevo == Genero.Masculino ? RelacionConLider.Esposo : RelacionConLider.Esposa,

        // Hijo/a → Padre/Madre
        RelacionConLider.Hijo or RelacionConLider.Hija
            => generoDelNuevo == Genero.Masculino ? RelacionConLider.Padre : RelacionConLider.Madre,

        // Padre/Madre → Hijo/Hija
        RelacionConLider.Padre or RelacionConLider.Madre
            => generoDelNuevo == Genero.Masculino ? RelacionConLider.Hijo : RelacionConLider.Hija,

        // Hermano/a ↔ Hermano/a
        RelacionConLider.Hermano or RelacionConLider.Hermana
            => generoDelNuevo == Genero.Masculino ? RelacionConLider.Hermano : RelacionConLider.Hermana,

        // Abuelo/a → Nieto/a
        RelacionConLider.Abuelo or RelacionConLider.Abuela
            => generoDelNuevo == Genero.Masculino ? RelacionConLider.Nieto : RelacionConLider.Nieta,

        // Nieto/a → Abuelo/a
        RelacionConLider.Nieto or RelacionConLider.Nieta
            => generoDelNuevo == Genero.Masculino ? RelacionConLider.Abuelo : RelacionConLider.Abuela,

        // Tío/a → Sobrino/a
        RelacionConLider.Tio or RelacionConLider.Tia
            => generoDelNuevo == Genero.Masculino ? RelacionConLider.Sobrino : RelacionConLider.Sobrina,

        // Sobrino/a → Tío/a
        RelacionConLider.Sobrino or RelacionConLider.Sobrina
            => generoDelNuevo == Genero.Masculino ? RelacionConLider.Tio : RelacionConLider.Tia,

        // Primo/a ↔ Primo/a
        RelacionConLider.Primo or RelacionConLider.Prima
            => generoDelNuevo == Genero.Masculino ? RelacionConLider.Primo : RelacionConLider.Prima,

        // Cuñado/a ↔ Cuñado/a
        RelacionConLider.Cunado or RelacionConLider.Cunada
            => generoDelNuevo == Genero.Masculino ? RelacionConLider.Cunado : RelacionConLider.Cunada,

        // Suegro/a → Yerno/Nuera
        RelacionConLider.Suegro or RelacionConLider.Suegra
            => generoDelNuevo == Genero.Masculino ? RelacionConLider.Yerno : RelacionConLider.Nuera,

        // Yerno/Nuera → Suegro/a
        RelacionConLider.Yerno or RelacionConLider.Nuera
            => generoDelNuevo == Genero.Masculino ? RelacionConLider.Suegro : RelacionConLider.Suegra,

        _ => RelacionConLider.Otro,
    };

    void RegistrarFamiliar(FamiliarCommand datos, int eventoId, int grupoId, bool requiereHospedaje, RelacionConLider relacion, bool uniendoseAGrupoExistente)
    {
        if (datos.PersonaExistenteId is int personaId)
        {
            var inscripcionExistente = db.Inscripciones
                .FirstOrDefault(i => i.PersonaId == personaId && i.EventoId == eventoId);

            if (inscripcionExistente is not null)
            {
                // Ya está inscrita — asegurar que esté en el grupo correcto.
                // No modificar su RelacionConLider si nos estamos uniendo a su grupo.
                inscripcionExistente.GrupoAsistenciaId = grupoId;
                if (!uniendoseAGrupoExistente)
                    inscripcionExistente.RelacionConLider = relacion;
            }
            else
            {
                var inscripcion = CrearInscripcion(personaId, eventoId, grupoId, requiereHospedaje, datos.NecesidadesEspeciales, relacion);
                db.Inscripciones.Add(inscripcion);
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

            var inscripcion = CrearInscripcion(persona, eventoId, grupoId, requiereHospedaje, datos.NecesidadesEspeciales, relacion);
            db.Inscripciones.Add(inscripcion);
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