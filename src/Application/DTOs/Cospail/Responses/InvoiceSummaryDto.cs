namespace Application.DTOs.Cospail.Responses;

/// <summary>
/// Resumen de una factura/cobro obtenido del reporte de cobros de Cospail
/// (<c>ObtenerCobrosFecha</c>) para la vista de facturas de los últimos 6 meses.
/// Campos reales SOAP: codCobrador, IDCredito, FechaPago, HoraPago, CodigoFijo, Nombre, Importe.
/// </summary>
public sealed class InvoiceSummaryDto
{
    /// <summary>
    /// Número de crédito (IDCredito del reporte). Se envía como NCredito
    /// a <c>obtenerUnaFacturaPDFB64</c> para obtener el PDF (son lo mismo).
    /// </summary>
    public int CreditNumber { get; set; }

    /// <summary>Fecha y hora del cobro (FechaPago + HoraPago combinadas).</summary>
    public DateTime? ChargeDate { get; set; }

    /// <summary>Hora del pago tal como la devuelve Cospail (HoraPago).</summary>
    public string PaymentTime { get; set; } = string.Empty;

    /// <summary>Importe del cobro (Importe).</summary>
    public decimal Amount { get; set; }

    /// <summary>Nombre del asociado (Nombre).</summary>
    public string MemberName { get; set; } = string.Empty;

    /// <summary>Código del cobrador (codCobrador).</summary>
    public int CollectorCode { get; set; }

    /// <summary>Código fijo del asociado (CodigoFijo).</summary>
    public int FixedCode { get; set; }
}
