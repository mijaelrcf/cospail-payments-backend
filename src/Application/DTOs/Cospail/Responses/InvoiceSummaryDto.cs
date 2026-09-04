namespace Application.DTOs.Cospail.Responses;

/// <summary>
/// Resumen de una factura/cobro obtenido del reporte de cobros de Cospail
/// (<c>ObtenerCobrosFecha</c>) para la vista de facturas de los últimos 6 meses.
/// </summary>
public sealed class InvoiceSummaryDto
{
    /// <summary>Número de crédito del aviso (NCredito). Se usa para pedir el PDF.</summary>
    public int CreditNumber { get; set; }

    /// <summary>Número de aviso (NAviso), cuando el reporte lo incluye.</summary>
    public int NoticeNumber { get; set; }

    /// <summary>Período del cobro (p. ej. "Abr/2023"), cuando el reporte lo incluye.</summary>
    public string Period { get; set; } = string.Empty;

    /// <summary>Fecha del cobro, cuando el reporte la incluye.</summary>
    public DateTime? ChargeDate { get; set; }

    /// <summary>Importe del cobro.</summary>
    public decimal Amount { get; set; }

    /// <summary>Nombre del asociado, cuando el reporte lo incluye.</summary>
    public string MemberName { get; set; } = string.Empty;

    /// <summary>Número de factura, cuando el reporte lo incluye.</summary>
    public string InvoiceNumber { get; set; } = string.Empty;
}
