using Application.DTOs.Cospail.Requests;
using Application.DTOs.Cospail.Responses;

namespace Application.Interfaces.External;

/// <summary>
/// Define las operaciones del servicio SOAP de Cospail.
/// </summary>
public interface ICospailSoapClient
{
    Task<GetMemberDebtByDocumentResponse> GetMemberDebtByDocumentAsync(
        int fixedCode,
        string documentId,
        CancellationToken cancellationToken = default
    );

    Task<RecordPaymentResponseDto> RecordPaymentAsync(
        RecordPaymentRequestDto requestDto,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Obtiene el reporte de cobros de un socio en un rango de fechas
    /// mediante <c>ObtenerCobrosFecha</c>.
    /// </summary>
    Task<List<InvoiceSummaryDto>> GetChargesByDateAsync(
        int fixedCode,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Obtiene el PDF (Base64) de una factura mediante <c>obtenerUnaFacturaPDFB64</c>.
    /// </summary>
    Task<InvoicePdfDto> GetInvoicePdfBase64Async(
        int creditNumber,
        CancellationToken cancellationToken = default
    );
}
