using Application.DTOs.Cospail;
using Application.Interfaces.External;
using Application.Interfaces.Services;
using CospailPaymentApi.Application.DTOs.Cospail;

namespace Application.Services;

/// <summary>
/// Servicio de aplicación que orquesta consultas al servicio de Cospail.
/// </summary>
public class CospailSoapService : ICospailSoapService
{
    private readonly ICospailSoapClient _cospailSoapClient;

    public CospailSoapService(ICospailSoapClient cospailSoapClient)
    {
        _cospailSoapClient = cospailSoapClient;
    }

    public async Task<CospailDebtResponseDto> GetDebtAsync(
        int fixedCode,
        CancellationToken cancellationToken = default
    )
    {
        if (fixedCode <= 0)
        {
            throw new ArgumentException("El código fijo debe ser mayor a cero.");
        }

        return await _cospailSoapClient.GetDebtByFixedCodeAsync(fixedCode, cancellationToken);
    }

    public async Task<GetMemberDebtByDocumentResponse> GetMemberDebtByDocumentAsync(
        int fixedCode,
        string documentId,
        CancellationToken cancellationToken = default
    )
    {
        if (fixedCode <= 0)
        {
            throw new ArgumentException("El código fijo debe ser mayor a cero.");
        }

        return await _cospailSoapClient.GetMemberDebtByDocumentAsync(
            fixedCode,
            documentId,
            cancellationToken
        );
    }
}
