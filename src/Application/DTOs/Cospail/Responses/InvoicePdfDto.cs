namespace Application.DTOs.Cospail.Responses;

/// <summary>
/// Factura en PDF (Base64) obtenida mediante <c>obtenerUnaFacturaPDFB64</c>.
/// </summary>
public sealed class InvoicePdfDto
{
    /// <summary>IDCredito de la factura solicitada (parámetro SOAP NCredito).</summary>
    public int CreditNumber { get; set; }

    /// <summary>Nombre de archivo sugerido para descarga.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Tipo de contenido (siempre <c>application/pdf</c>).</summary>
    public string ContentType { get; set; } = "application/pdf";

    /// <summary>Contenido del PDF codificado en Base64.</summary>
    public string PdfBase64 { get; set; } = string.Empty;
}
