using AgendaPositiva.Web.Features.Inscripciones.Dominio;

namespace AgendaPositiva.Web.Features.Admin.Inscripciones.Views.ViewModels;

public class VerificarFamiliaresViewModel
{
    public int InscripcionId { get; set; }
    public string NombrePersona { get; set; } = "";
    public Persona? PersonaEncontrada { get; set; }
    public List<Inscripcion> FamiliaresAgregados { get; set; } = [];
}
