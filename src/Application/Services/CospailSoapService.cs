using Application.DTOs.Cospail.Common;
using Application.DTOs.Cospail.Requests;
using Application.DTOs.Cospail.Responses;
using Application.Interfaces.External;
using Application.Interfaces.Internal;
using Domain.Entities;
using FluentValidation;

namespace Application.Services;

/// <summary>
/// Servicio de aplicación que orquesta consultas al servicio de Cospail.
/// </summary>
public sealed class CospailSoapService(
    ICospailSoapClient cospailSoapClient,
    IValidator<ConfirmPaymentRequestDto> confirmPaymentValidator
) : ICospailSoapService
{
    public async Task<CospailDebtResponseDto> GetDebtAsync(
        int fixedCode,
        CancellationToken cancellationToken = default
    )
    {
        if (fixedCode <= 0)
        {
            throw new ArgumentException("El código fijo debe ser mayor a cero.");
        }

        return await cospailSoapClient.GetDebtByFixedCodeAsync(fixedCode, cancellationToken);
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

        return await cospailSoapClient.GetMemberDebtByDocumentAsync(
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
        ArgumentNullException.ThrowIfNull(request);

        await confirmPaymentValidator.ValidateAndThrowAsync(request, cancellationToken);

        var debtResponse = await cospailSoapClient.GetMemberDebtByDocumentAsync(
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

        var paymentDateTime = DateTime.UtcNow;

        var recordPaymentResponse = await cospailSoapClient.RecordPaymentAsync(
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
