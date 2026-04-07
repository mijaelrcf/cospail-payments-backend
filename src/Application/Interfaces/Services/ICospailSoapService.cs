using Application.DTOs.Cospail;
using CospailPaymentApi.Application.DTOs.Cospail;

namespace Application.Interfaces.Services;

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
}
