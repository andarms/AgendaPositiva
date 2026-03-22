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
        await db.SaveChangesAsync();
    }
}
