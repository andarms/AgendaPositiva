namespace AgendaPositiva.Web.Features.Inscripciones.Views;

public class PersonaFieldsViewModel
{
    public string Prefix { get; set; } = "";
    public bool DisableIdentificacion { get; set; }
    public bool ShowRelacion { get; set; }
    public string? TipoIdentificacion { get; set; }
    public string? NumeroIdentificacion { get; set; }
}
