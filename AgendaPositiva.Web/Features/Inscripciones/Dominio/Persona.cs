using AgendaPositiva.Web.Features.Commons;
using AgendaPositiva.Web.Features.Commons.Domain;

namespace AgendaPositiva.Web.Features.Inscripciones.Dominio;

public class Persona : EntidadBase
{
    public string Nombres { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public Genero Genero { get; set; }
    public DateOnly FechaNacimiento { get; set; }
    public string Telefono { get; set; } = string.Empty;
    public string? Email { get; set; }
    public TipoIdentificacion TipoIdentificacion { get; set; }
    public string NumeroIdentificacion { get; set; } = string.Empty;
    public ICollection<Inscripcion> Inscripciones { get; set; } = [];

    public string NombreCompleto => $"{Nombres} {Apellidos}";

    public float Edad => CalcularEdad();
    public bool EsMayorDeEdad => Edad >= 18;
    public bool EsNino => Edad <= 10;

    public int CalcularEdad()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        int age = today.Year - FechaNacimiento.Year;
        if (FechaNacimiento > today.AddYears(-age)) age--;
        return Math.Max(age, 0);
    }
}