using Application.DTOs.Cospail.Requests;
using Application.DTOs.Cospail.Responses;

namespace Application.Interfaces.External;

/// <summary>
/// Define las operaciones del servicio SOAP de Cospail.
/// </summary>
public interface ICospailSoapClient
{
    Task<CospailDebtResponseDto> GetDebtByFixedCodeAsync(
        int fixedCode,
        CancellationToken cancellationToken = default
    );

    Task<GetMemberDebtByDocumentResponse> GetMemberDebtByDocumentAsync(
        int fixedCode,
        string documentId,
        CancellationToken cancellationToken = default
    );

    Task<RecordPaymentResponseDto> RecordPaymentAsync(
        RecordPaymentRequestDto requestDto,
        CancellationToken cancellationToken = default
    );
}
