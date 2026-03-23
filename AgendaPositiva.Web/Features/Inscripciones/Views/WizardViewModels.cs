using AgendaPositiva.Web.Features.Inscripciones.Dominio;

namespace AgendaPositiva.Web.Features.Inscripciones.Views;

public class VerificarParejaViewModel
{
    public int InscripcionId { get; set; }
    public Persona? PersonaEncontrada { get; set; }
}

public class VerificarFamiliaresViewModel
{
    public int InscripcionId { get; set; }
    public Persona? PersonaEncontrada { get; set; }
    public List<Inscripcion> FamiliaresAgregados { get; set; } = [];
}
