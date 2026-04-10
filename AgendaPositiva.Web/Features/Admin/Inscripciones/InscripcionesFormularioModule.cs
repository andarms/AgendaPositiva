using AgendaPositiva.Web.Features.Inscripciones.Dominio;
using AgendaPositiva.Web.Features.Inscripciones.Operaciones;

namespace AgendaPositiva.Web.Features.Admin.Inscripciones;

public static class InscripcionesModule
{
    public static IServiceCollection AgregarModuloInscripciones(this IServiceCollection services)
    {
        // Data
        services.AddScoped<RegistrarPersonaPrincipal>();
        services.AddScoped<AgregarAlGrupo>();

        // Servicios
        services.AddSingleton<UbicacionService>();

        return services;
    }
}