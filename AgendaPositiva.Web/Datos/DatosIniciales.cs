using AgendaPositiva.Web.Features.Admin.Auth.Domain;
using AgendaPositiva.Web.Features.Inscripciones.Dominio;
using Microsoft.EntityFrameworkCore;

namespace AgendaPositiva.Web.Datos;

public static class DatosIniciales
{
    public static async Task AlimentarAsync(AppDbContext db, IWebHostEnvironment env)
    {
        if (await db.Eventos.AnyAsync()) return;

        var evento = new Evento
        {
            Nombre = "Conferencia Internacional Colombia 2026",
            Descripcion = "Conferencia Internacional Colombia 2026",
            FechaInicio = new DateTime(2026, 8, 22, 8, 0, 0, DateTimeKind.Utc),
            FechaFin = new DateTime(2026, 8, 23, 18, 0, 0, DateTimeKind.Utc),
            Ubicacion = "Centro de Convenciones",
            Activo = true
        };

        db.Eventos.Add(evento);

        var todosDepartamentos = UbicacionService.ObtenerTodosLosDepartamentos(env);

        var administrador1 = new UsuarioAdministrador
        {
            Email = "and7702@gmail.com",
            Nombre = "Adrian Manjarres",
            Rol = RolAdministrador.Administrador,
            Departamentos = todosDepartamentos,
            Activo = true
        };
        db.UsuariosAdministradores.Add(administrador1);

        var administrador2 = new UsuarioAdministrador
        {
            Email = "adrian.manjarres@avant.com",
            Nombre = "Adrian Manjarres",
            Rol = RolAdministrador.Colaborador,
            Departamentos = ["Cundinamarca"],
            Activo = true
        };
        db.UsuariosAdministradores.Add(administrador2);

        await db.SaveChangesAsync();

    }
}
