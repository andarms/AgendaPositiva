using System.Text.RegularExpressions;
using AgendaPositiva.Web.Features.Commons;

namespace AgendaPositiva.Web.Features.Commons.Views;

public static partial class ViewHelpers
{
    /// <summary>
    /// Convierte PascalCase a palabras separadas con la primera en mayúscula.
    /// Ej: "CedulaExtranjeria" → "Cédula Extranjeria"
    /// </summary>
    public static string Humanize(this Enum value)
    {
        var name = value.ToString();
        return PascalCaseRegex().Replace(name, " $1");
    }

    /// <summary>
    /// Convierte PascalCase a Title Case (cada palabra capitalizada).
    /// Ej: "NoAsistio" → "No Asistio"
    /// </summary>
    public static string Titalize(this Enum value)
    {
        return value.Humanize();
    }

    public static string BadgeCss(this EstadoInscripcion estado) => estado switch
    {
        EstadoInscripcion.Completado => "badge--success",
        EstadoInscripcion.Abono2 => "badge--abono2",
        EstadoInscripcion.Abono1 => "badge--abono1",
        EstadoInscripcion.Pendiente => "badge--warning",
        EstadoInscripcion.NoVaAsistir => "badge--danger",
        _ => "badge--muted"
    };

    public static int CalcularEdad(this DateOnly fechaNacimiento)
    {
        var hoy = DateOnly.FromDateTime(DateTime.Today);
        var edad = hoy.Year - fechaNacimiento.Year;
        if (hoy < fechaNacimiento.AddYears(edad))
            edad--;
        return edad;
    }

    [GeneratedRegex(@"(?<!^)([A-Z])")]
    private static partial Regex PascalCaseRegex();
}
