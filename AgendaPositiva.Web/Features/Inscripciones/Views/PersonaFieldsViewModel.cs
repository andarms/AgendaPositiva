namespace AgendaPositiva.Web.Features.Inscripciones.Views;

public class PersonaFieldsViewModel
{
    public string Prefix { get; set; } = "";
    public bool IdentificacionHabilitada { get; set; }
    public bool MostrarRelacion { get; set; }
    public string? TipoIdentificacion { get; set; }
    public string? NumeroIdentificacion { get; set; }
}
