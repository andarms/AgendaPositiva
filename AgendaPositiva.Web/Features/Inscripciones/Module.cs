using AgendaPositiva.Web.Features.Inscripciones.Operaciones;

namespace AgendaPositiva.Web.Features.Inscripciones;

public static class InscripcionesModule
{
    public static IServiceCollection AgregarModuloInscripciones(this IServiceCollection services)
    {
        // Data
        services.AddScoped<RegistrarPreInscricion>();
        services.AddScoped<RegistrarPersonaPrincipal>();
        services.AddScoped<AgregarAlGrupo>();

        return services;
    }
}