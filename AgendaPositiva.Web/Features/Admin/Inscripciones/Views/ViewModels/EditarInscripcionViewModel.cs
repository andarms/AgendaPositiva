using AgendaPositiva.Web.Features.Commons;
using AgendaPositiva.Web.Features.Inscripciones.Dominio;

namespace AgendaPositiva.Web.Features.Admin.Inscripciones.Views.ViewModels;

public class EditarInscripcionViewModel
{
    public required Inscripcion Inscripcion { get; set; }
    public List<string> DepartamentosDisponibles { get; set; } = [];
}
