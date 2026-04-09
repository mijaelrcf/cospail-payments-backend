using Application.DTOs.Cospail.Common;
using Application.DTOs.Cospail.Requests;
using Application.DTOs.Cospail.Responses;
using Application.Interfaces.External;
using Application.Interfaces.Internal;

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

    public async Task<ConfirmPaymentResponseDto> ConfirmPaymentAsync(
        ConfirmPaymentRequestDto request,
        CancellationToken cancellationToken = default
    )
    {
        var debtResponse = await _cospailSoapClient.GetMemberDebtByDocumentAsync(
            request.FixedCode,
            request.DocumentId,
            cancellationToken
        );

        if (debtResponse.Status == MemberDebtStatus.MemberNotFound)
        {
            throw new InvalidOperationException("El socio no existe en Cospail.");
        }

        if (debtResponse.Status == MemberDebtStatus.DocumentMismatch)
        {
            throw new InvalidOperationException("El documento no coincide con el código fijo.");
        }

        if (debtResponse.Status == MemberDebtStatus.NoDebt)
        {
            throw new InvalidOperationException("El socio no tiene deudas pendientes.");
        }

        var debtToPay = debtResponse.Debts.FirstOrDefault(x =>
            x.CreditNumber == request.CreditNumber
            && x.Type == request.Type
            && x.Amount == request.Amount
        );

        if (debtToPay is null)
        {
            throw new InvalidOperationException(
                "La deuda enviada no coincide con la deuda registrada en Cospail."
            );
        }

        var paymentDateTime = DateTime.Now;

        var recordPaymentResponse = await _cospailSoapClient.RecordPaymentAsync(
            new RecordPaymentRequestDto
            {
                CreditNumber = request.CreditNumber,
                Type = request.Type,
                Amount = request.Amount,
                PaymentDate = paymentDateTime,
                PaymentTime = paymentDateTime.ToString("HH:mm:ss")
            },
            cancellationToken
        );

        return new ConfirmPaymentResponseDto
        {
            Success = recordPaymentResponse.Success,
            Message = recordPaymentResponse.Message,
            FixedCode = request.FixedCode,
            DocumentId = request.DocumentId,
            CreditNumber = request.CreditNumber,
            Type = request.Type,
            Amount = request.Amount,
            MemberName = debtResponse.MemberName,
            RawCospailResult = recordPaymentResponse.RawResult
        };
    }
}
