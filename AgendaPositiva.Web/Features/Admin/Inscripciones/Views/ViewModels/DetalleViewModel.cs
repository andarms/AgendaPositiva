using AgendaPositiva.Web.Features.Admin.Auditoria;
using AgendaPositiva.Web.Features.Inscripciones.Dominio;

namespace AgendaPositiva.Web.Features.Admin.Inscripciones.Views.ViewModels;

public class DetalleViewModel
{
    public required Inscripcion Inscripcion { get; set; }
    public List<AuditoriaAdmin> Auditoria { get; set; } = [];
}
