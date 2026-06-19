using System.ComponentModel.DataAnnotations;
using System.Reflection;
using AgendaPositiva.Web.Features.Commons.Domain;
using AgendaPositiva.Web.Features.Inscripciones.Dominio;

namespace AgendaPositiva.Web.Features.Admin.Servicios.Dominio;

public enum RolMiembroGrupoServicio
{
    Coordinador,
    Lider,
    Servidor,
    [Display(Name = "Coordinador Regional")] CoordinadorRegional,
    [Display(Name = "Coordinador local")] CoordinadorLocal,
    [Display(Name = "Tutor con E. De 4 a 5")] TutorConE4a5,
    [Display(Name = "Tutor con E. De 6 a 8")] TutorConE6a8,
    [Display(Name = "Tutor con E. De 9 a 10")] TutorConE9a10,
    [Display(Name = "Hermanos Intendentes")] HermanosIntendentes,
    [Display(Name = "Adolescentes")] Adolescentes,
    [Display(Name = "Tutores sin E.")] TutoresSinE,
    [Display(Name = "Jóvenes adultos varones")] JovenesAdultosVarones,
    [Display(Name = "Tutores de las 3 edades")] TutoresDeLas3Edades,
    [Display(Name = "Coord general J N° 1")] CoordGeneralJ1,
    [Display(Name = "Coord general J N° 2")] CoordGeneralJ2,
    [Display(Name = "Coord general J N° 3")] CoordGeneralJ3
}

public static class RolMiembroGrupoServicioExtensions
{
    /// <summary>Nombre legible del rol (usa el atributo Display si existe).</summary>
    public static string NombreParaMostrar(this RolMiembroGrupoServicio rol)
    {
        var miembro = typeof(RolMiembroGrupoServicio).GetMember(rol.ToString()).FirstOrDefault();
        return miembro?.GetCustomAttribute<DisplayAttribute>()?.Name ?? rol.ToString();
    }

    /// <summary>
    /// Intenta parsear un rol desde nombre de enum, texto de Display o valor numérico.
    /// </summary>
    public static bool TryParseFlexible(string? value, out RolMiembroGrupoServicio rol)
    {
        rol = default;
        if (string.IsNullOrWhiteSpace(value)) return false;

        var input = value.Trim();

        if (Enum.TryParse<RolMiembroGrupoServicio>(input, ignoreCase: true, out var byName))
        {
            rol = byName;
            return true;
        }

        if (int.TryParse(input, out var numeric) && Enum.IsDefined(typeof(RolMiembroGrupoServicio), numeric))
        {
            rol = (RolMiembroGrupoServicio)numeric;
            return true;
        }

        foreach (RolMiembroGrupoServicio candidate in Enum.GetValues(typeof(RolMiembroGrupoServicio)))
        {
            if (string.Equals(candidate.NombreParaMostrar(), input, StringComparison.OrdinalIgnoreCase))
            {
                rol = candidate;
                return true;
            }
        }

        return false;
    }
}

public class MiembroGrupoServicio : EntidadBase
{
    public int GrupoServicioId { get; set; }
    public int InscripcionId { get; set; }
    public int? HorarioServicioId { get; set; }
    public int? UbicacionServicioId { get; set; }
    public GrupoServicio GrupoServicio { get; set; } = null!;
    public Inscripcion Inscripcion { get; set; } = null!;
    public HorarioServicio? HorarioServicio { get; set; }
    public UbicacionServicio? UbicacionServicio { get; set; }
    public RolMiembroGrupoServicio Rol { get; set; }
}
