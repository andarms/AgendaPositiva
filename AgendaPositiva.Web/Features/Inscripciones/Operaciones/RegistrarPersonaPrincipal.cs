using AgendaPositiva.Web.Datos;
using AgendaPositiva.Web.Features.Commons;
using AgendaPositiva.Web.Features.Inscripciones.Dominio;
using Microsoft.EntityFrameworkCore;

namespace AgendaPositiva.Web.Features.Inscripciones.Operaciones;

public record RegistrarPersonaPrincipalDto(
    string Nombres,
    string Apellidos,
    Genero Genero,
    DateOnly FechaNacimiento,
    string Telefono,
    string? Email,
    TipoIdentificacion TipoIdentificacion,
    string NumeroIdentificacion,
    bool RequiereHospedaje,
    List<ServicioInscripcion>? Servicios,
    string? NecesidadesEspeciales,
    string Ciudad,
    string Departamento
);

public class RegistrarPersonaPrincipal
{
    readonly AppDbContext db;

    public RegistrarPersonaPrincipal(AppDbContext db)
    {
        this.db = db;
    }

    public async Task<Result<int>> Handle(RegistrarPersonaPrincipalDto command)
    {
        Evento? evento = db.Eventos.FirstOrDefault(e => e.Activo);
        if (evento is null)
            return Result.Failure<int>("No hay un evento activo.");

        // Buscar si ya existe una persona con esa identificación
        var persona = db.Personas.FirstOrDefault(p =>
            p.TipoIdentificacion == command.TipoIdentificacion &&
            p.NumeroIdentificacion == command.NumeroIdentificacion);

        if (persona is not null)
        {
            // Actualizar datos de la persona existente
            persona.Nombres = command.Nombres;
            persona.Apellidos = command.Apellidos;
            persona.Genero = command.Genero;
            persona.FechaNacimiento = command.FechaNacimiento;
            persona.Telefono = command.Telefono;
            persona.Email = command.Email;
        }
        else
        {
            persona = new Persona
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
        }
        await db.SaveChangesAsync();

        // Buscar si ya existe una inscripción para esta persona en el evento activo
        var inscripcion = db.Inscripciones
            .FirstOrDefault(i => i.PersonaId == persona.Id && i.EventoId == evento.Id);

        if (inscripcion is not null)
        {
            // Actualizar datos de la inscripción existente
            inscripcion.RequiereHospedaje = command.RequiereHospedaje;
            inscripcion.Servicios = command.Servicios ?? [];
            inscripcion.NecesidadesEspeciales = command.NecesidadesEspeciales;
            inscripcion.Ciudad = command.Ciudad;
            inscripcion.Departamento = command.Departamento;
        }
        else
        {
            // No se crea grupo familiar aún — se creará cuando se agregue un familiar.
            // Si viaja solo, la inscripción queda sin grupo.
            inscripcion = new Inscripcion
            {
                PersonaId = persona.Id,
                EventoId = evento.Id,
                GrupoFamiliarId = null,
                RequiereHospedaje = command.RequiereHospedaje,
                Servicios = command.Servicios ?? [],
                NecesidadesEspeciales = command.NecesidadesEspeciales,
                Ciudad = command.Ciudad,
                Departamento = command.Departamento,
            };
            db.Inscripciones.Add(inscripcion);
        }
        await db.SaveChangesAsync();

        return Result.Success(inscripcion.Id);
    }
}
