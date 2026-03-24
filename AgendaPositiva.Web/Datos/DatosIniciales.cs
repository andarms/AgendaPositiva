using AgendaPositiva.Web.Features.Admin.Auth.Domain;
using AgendaPositiva.Web.Features.Inscripciones.Dominio;
using Microsoft.EntityFrameworkCore;

namespace AgendaPositiva.Web.Datos;

public static class DatosIniciales
{
    public static async Task AlimentarAsync(AppDbContext db)
    {
        if (await db.Eventos.AnyAsync()) return;

        var evento = new Evento
        {
            Nombre = "Agenda Positiva",
            Descripcion = "Conferencia Agenda Positiva edición",
            FechaInicio = new DateTime(2026, 6, 1, 8, 0, 0, DateTimeKind.Utc),
            FechaFin = new DateTime(2026, 6, 3, 18, 0, 0, DateTimeKind.Utc),
            Ubicacion = "Centro de Convenciones",
            Activo = true
        };

        db.Eventos.Add(evento);

        var administrador1 = new UsuarioAdministrador
        {
            Email = "and7702@gmail.com",
            Nombre = "Adrian Manjarres",
            Activo = true
        };
        db.UsuariosAdministradores.Add(administrador1);

        var administrador2 = new UsuarioAdministrador
        {
            Email = "adrian.manjarres@avant.com",
            Nombre = "Adrian Manjarres",
            Activo = true
        };
        db.UsuariosAdministradores.Add(administrador2);

        await db.SaveChangesAsync();

    }
}
