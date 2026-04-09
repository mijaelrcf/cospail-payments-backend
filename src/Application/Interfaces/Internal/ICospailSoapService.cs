using Application.DTOs.Cospail.Requests;
using Application.DTOs.Cospail.Responses;

namespace Application.Interfaces.Internal;

/// <summary>
/// Servicio de aplicación para consultas relacionadas a Cospail.
/// </summary>
public interface ICospailSoapService
{
    Task<CospailDebtResponseDto> GetDebtAsync(
        int fixedCode,
        CancellationToken cancellationToken = default
    );

    Task<GetMemberDebtByDocumentResponse> GetMemberDebtByDocumentAsync(
        int fixedCode,
        string documentId,
        CancellationToken cancellationToken = default
    );

    Task<ConfirmPaymentResponseDto> ConfirmPaymentAsync(
        ConfirmPaymentRequestDto request,
        CancellationToken cancellationToken = default
    );
}
