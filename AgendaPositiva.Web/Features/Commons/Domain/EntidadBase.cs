namespace AgendaPositiva.Web.Features.Commons.Domain;

/// <summary>
/// Clase base con campos de auditoría compartidos por todas las entidades.
/// </summary>
public abstract class EntidadBase
{
    public int Id { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaActualizacion { get; set; }
}