namespace AgendaPositiva.Web.Features.Inscripciones.Operaciones;

public sealed record ComandoCrearEvento(
    string Nombre,
    string? Slug,
    string? Descripcion,
    DateTime FechaInicio,
    DateTime FechaFin,
    string Ubicacion,
    bool Activo
);
