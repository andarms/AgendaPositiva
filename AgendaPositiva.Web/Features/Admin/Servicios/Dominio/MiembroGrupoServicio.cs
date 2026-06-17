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
