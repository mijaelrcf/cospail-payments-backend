using Application.DTOs.Cospail.Common;
using Application.DTOs.Cospail.Requests;
using Application.DTOs.Cospail.Responses;
using Application.Interfaces.External;
using Application.Interfaces.Internal;
using Application.Interfaces.Persistence;
using Domain.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

/// <summary>
/// Servicio de aplicación que orquesta consultas al servicio de Cospail.
/// </summary>
public sealed class CospailService(
    ICospailSoapClient cospailSoapClient,
    IPaymentsDbContext dbContext,
    IValidator<ConfirmPaymentRequestDto> confirmPaymentValidator,
    IValidator<InitiatePaymentRequestDto> initiatePaymentValidator
) : ICospailService
{
    private static readonly TimeZoneInfo BoliviaTimeZone = GetBoliviaTimeZone();
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
            throw new ArgumentException("El socio no existe en Cospail.");
        }

        if (debtResponse.Status == MemberDebtStatus.DocumentMismatch)
        {
            throw new ArgumentException("El documento no coincide con el código fijo.");
        }

        if (debtResponse.Status == MemberDebtStatus.NoDebt)
        {
            throw new ArgumentException("El socio no tiene deudas pendientes.");
        }

        var debtToPay = debtResponse.Debts.FirstOrDefault(x =>
            x.CreditNumber == request.CreditNumber
            && x.Type == request.Type
            && x.Amount == request.Amount
        );

        if (debtToPay is null)
        {
            throw new ArgumentException(
                "La deuda enviada no coincide con la deuda registrada en Cospail."
            );
        }

        var paymentDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, BoliviaTimeZone);

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
            MemberName = debtResponse.MemberName
        };
    }

    public async Task<RecordPaymentResponseDto> RecordDebtPaymentAsync(
        int creditNumber,
        int type,
        decimal amount,
        CancellationToken cancellationToken = default
    )
    {
        var paymentDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, BoliviaTimeZone);

        return await cospailSoapClient.RecordPaymentAsync(
            new RecordPaymentRequestDto
            {
                CreditNumber = creditNumber,
                Type = type,
                Amount = amount,
                PaymentDate = paymentDateTime,
                PaymentTime = paymentDateTime.ToString("HH:mm:ss")
            },
            cancellationToken
        );
    }

    public async Task<PagoCospailResponseDto> InitiatePaymentAsync(
        InitiatePaymentRequestDto request,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        request.DocumentId = request.DocumentId.Trim();

        await initiatePaymentValidator.ValidateAndThrowAsync(request, cancellationToken);

        var debtResponse = await cospailSoapClient.GetMemberDebtByDocumentAsync(
            request.FixedCode,
            request.DocumentId,
            cancellationToken
        );

        if (debtResponse.Status == MemberDebtStatus.MemberNotFound)
        {
            throw new ArgumentException("El socio no existe en Cospail.");
        }

        if (debtResponse.Status == MemberDebtStatus.DocumentMismatch)
        {
            throw new ArgumentException("El documento no coincide con el código fijo.");
        }

        if (debtResponse.Status == MemberDebtStatus.NoDebt)
        {
            throw new ArgumentException("El socio no tiene deudas pendientes.");
        }

        var availableDebts = debtResponse.Debts.ToList();
        var selectedDebts = new List<DebtItemDto>();

        foreach (var item in request.Debts)
        {
            var index = availableDebts.FindIndex(x =>
                x.CreditNumber == item.CreditNumber
                && x.Type == item.Type
                && x.Amount == item.Amount
            );

            if (index < 0)
            {
                throw new ArgumentException(
                    $"La deuda {item.CreditNumber} (tipo {item.Type}) no coincide con la deuda registrada en Cospail."
                );
            }

            selectedDebts.Add(availableDebts[index]);
            availableDebts.RemoveAt(index);
        }

        var totalAmount = selectedDebts.Sum(x => x.Amount);

        var pagoCospail = new PagoCospail(
            request.FixedCode,
            request.DocumentId,
            debtResponse.MemberName,
            totalAmount,
            DateTime.UtcNow
        );

        foreach (var deuda in selectedDebts)
        {
            pagoCospail.AddDeuda(
                new DeudaCospail(
                    request.FixedCode,
                    request.DocumentId,
                    debtResponse.MemberName,
                    deuda.CreditNumber,
                    deuda.Type,
                    deuda.NoticeNumber,
                    deuda.Year,
                    deuda.Month,
                    deuda.Period,
                    deuda.Amount
                )
            );
        }

        dbContext.PagosCospail.Add(pagoCospail);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(pagoCospail);
    }

    public async Task<PagoCospailResponseDto> GetPaymentStatusAsync(
        Guid pagoCospailId,
        CancellationToken cancellationToken = default
    )
    {
        var pagoCospail = await dbContext
            .PagosCospail.Include(x => x.Deudas)
            .SingleOrDefaultAsync(x => x.Id == pagoCospailId, cancellationToken);

        if (pagoCospail is null)
        {
            throw new KeyNotFoundException(
                "No se encontró un pago con el pagoCospailId proporcionado."
            );
        }

        return ToResponse(pagoCospail);
    }

    private static PagoCospailResponseDto ToResponse(PagoCospail pagoCospail) =>
        new()
        {
            PagoCospailId = pagoCospail.Id,
            FixedCode = pagoCospail.FixedCode,
            DocumentId = pagoCospail.DocumentId,
            MemberName = pagoCospail.MemberName,
            TotalAmount = pagoCospail.TotalAmount,
            Status = pagoCospail.Status,
            CreatedAtUtc = pagoCospail.CreatedAtUtc,
            Debts = pagoCospail
                .Deudas.Select(x => new PagoDebtResponseDto
                {
                    CreditNumber = x.CreditNumber,
                    Type = x.Type,
                    NoticeNumber = x.NoticeNumber,
                    Year = x.Year,
                    Month = x.Month,
                    Period = x.Period,
                    MemberName = x.MemberName,
                    Amount = x.Amount,
                    Status = x.Status
                })
                .ToList()
        };

    private static TimeZoneInfo GetBoliviaTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/La_Paz");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.CreateCustomTimeZone("Bolivia", TimeSpan.FromHours(-4), "Bolivia", "Bolivia");
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.CreateCustomTimeZone("Bolivia", TimeSpan.FromHours(-4), "Bolivia", "Bolivia");
        }
    }
}
