using AgendaPositiva.Web.Features.Admin.Auditoria;
using AgendaPositiva.Web.Features.Inscripciones.Dominio;

namespace AgendaPositiva.Web.Features.Admin.Inscripciones.Views.ViewModels;

public class DetalleViewModel
{
    public required Inscripcion Inscripcion { get; set; }
    public List<AuditoriaAdmin> Auditoria { get; set; } = [];
    public List<CategoriaInscripcion> CategoriasDisponibles { get; set; } = [];

    // Calculados
    public decimal TotalAbonado => Inscripcion.Abonos.Sum(a => a.Monto);
    public decimal? PrecioCategoria => Inscripcion.CategoriaInscripcion?.Precio;
    public double PorcentajePago => PrecioCategoria > 0
        ? (double)(TotalAbonado / PrecioCategoria.Value) * 100
        : 0;

    public string EstadoPagoTexto => PorcentajePago switch
    {
        >= 100 => "Completado",
        > 0 => $"Abono ({PorcentajePago:F0}%)",
        _ => "Pendiente"
    };

    public string EstadoPagoBadgeCss => PorcentajePago switch
    {
        >= 100 => "badge--success",
        >= 50 => "badge--abono2",
        > 0 => "badge--abono1",
        _ => "badge--warning"
    };
}
