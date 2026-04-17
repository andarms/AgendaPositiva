using AgendaPositiva.Web.Datos;
using AgendaPositiva.Web.Features.Admin.Regiones.Dominio;
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
    bool? RequiereHospedaje,
    List<ServicioInscripcion>? Servicios,
    string? NecesidadesEspeciales,
    string Ciudad,
    string Departamento,
    bool TieneAlergiaAlimentaria = false,
    string? DescripcionAlergia = null,
    bool ParticipaComunionAncianos = false,
    bool RequiereAlimentacion = false,
    PreguntasAdicionalesNino? PreguntasAdicionalesNino = null
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
        if (command.RequiereHospedaje is null)
            return Result.Failure<int>("El campo '¿Requiere hospedaje?' es obligatorio.");

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

        bool esNuevaInscripcion = inscripcion is null;

        if (inscripcion is not null)
        {
            // Actualizar datos de la inscripción existente
            inscripcion.RequiereHospedaje = command.RequiereHospedaje.Value;
            inscripcion.Servicios = command.Servicios ?? [];
            inscripcion.NecesidadesEspeciales = command.NecesidadesEspeciales;
            inscripcion.Ciudad = command.Ciudad;
            inscripcion.Departamento = command.Departamento;
            inscripcion.TieneAlergiaAlimentaria = command.TieneAlergiaAlimentaria;
            inscripcion.DescripcionAlergia = command.DescripcionAlergia;
            inscripcion.ParticipaComunionAncianos = command.ParticipaComunionAncianos;
            inscripcion.RequiereAlimentacion = command.RequiereAlimentacion;
            inscripcion.PreguntasAdicionalesNino = command.PreguntasAdicionalesNino;
        }
        else
        {
            // Los cupos solo aplican para mayores de 10 años
            var esAdulto = command.FechaNacimiento.AddYears(10) <= DateOnly.FromDateTime(DateTime.UtcNow);

            if (esAdulto)
            {
                // Validar cupo del evento
                if (!evento.TieneCupo)
                    return Result.Failure<int>("Se ha alcanzado el cupo total del evento. No se pueden registrar más inscripciones.");

                // Validar cupo de región
                var regionError = await ValidarCupoRegion(evento.Id, command.Departamento, command.Ciudad);
                if (regionError is not null)
                    return Result.Failure<int>(regionError);
            }

            // No se crea grupo familiar aún — se creará cuando se agregue un familiar.
            // Si viaja solo, la inscripción queda sin grupo.
            inscripcion = new Inscripcion
            {
                PersonaId = persona.Id,
                EventoId = evento.Id,
                GrupoFamiliarId = null,
                RequiereHospedaje = command.RequiereHospedaje.Value,
                Servicios = command.Servicios ?? [],
                NecesidadesEspeciales = command.NecesidadesEspeciales,
                Ciudad = command.Ciudad,
                Departamento = command.Departamento,
                TieneAlergiaAlimentaria = command.TieneAlergiaAlimentaria,
                DescripcionAlergia = command.DescripcionAlergia,
                ParticipaComunionAncianos = command.ParticipaComunionAncianos,
                RequiereAlimentacion = command.RequiereAlimentacion,
                PreguntasAdicionalesNino = command.PreguntasAdicionalesNino,
            };
            db.Inscripciones.Add(inscripcion);

            // Incrementar contadores de cupo (solo mayores de 10 años)
            if (esAdulto)
            {
                evento.TotalInscritos++;
                await IncrementarCupoRegion(evento.Id, command.Departamento, command.Ciudad);
            }
        }
        await db.SaveChangesAsync();

        return Result.Success(inscripcion.Id);
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
