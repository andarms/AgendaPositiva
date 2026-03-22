using System.Text.RegularExpressions;

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

    [GeneratedRegex(@"(?<!^)([A-Z])")]
    private static partial Regex PascalCaseRegex();
}
