using Application.DTOs.BancoEconomico.Requests;
using Application.DTOs.BancoEconomico.Responses;
using Application.Interfaces.External;
using Application.Interfaces.Internal;
using Application.Interfaces.Persistence;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Services;

/// <summary>
/// Servicio de aplicación para operaciones con Banco Económico.
/// </summary>
public sealed class BancoEconomicoService(
    IBancoEconomicoQrClient bancoEconomicoQrClient,
    IPaymentsDbContext dbContext,
    ILogger<BancoEconomicoService> logger
) : IBancoEconomicoService
{
    private static readonly string[] ValidCurrencies = ["BOB", "USD"];

    /// <inheritdoc />
    public Task<AuthenticateResponseDto> AuthenticateAsync(CancellationToken cancellationToken = default) =>
        bancoEconomicoQrClient.AuthenticateAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<GenerateQrResponseDto> GenerateQrAsync(
        GenerateQrRequestDto request,
        CancellationToken cancellationToken = default
    )
    {
        ValidateGenerateQrRequest(request);

        if (await dbContext.PagosQr.SingleOrDefaultAsync(x => x.TransactionId == request.TransactionId, cancellationToken) is not null)
        {
            throw new ArgumentException("Ya existe un QR registrado para el transactionId proporcionado.");
        }

        logger.LogInformation(
            "Solicitando generación de QR. TransactionId: {TransactionId}",
            request.TransactionId
        );

        var auth = await bancoEconomicoQrClient.AuthenticateAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(auth.Token))
        {
            throw new InvalidOperationException(
                "No se recibió token de autenticación desde Banco Económico."
            );
        }

        var response = await bancoEconomicoQrClient.GenerateQrAsync(
            auth.Token,
            request,
            cancellationToken
        );

        if (string.IsNullOrWhiteSpace(response.QrId))
        {
            throw new InvalidOperationException("Banco Económico no devolvió un qrId para el QR generado.");
        }

        var pagoQr = new PagoQr(
            request.TransactionId.Trim(),
            response.QrId.Trim(),
            request.Amount,
            request.Currency,
            DateOnly.Parse(request.DueDate),
            request.SingleUse,
            request.ModifyAmount,
            request.Description?.Trim(),
            request.BranchCode?.Trim(),
            DateTime.UtcNow
        );

        dbContext.PagosQr.Add(pagoQr);
        await dbContext.SaveChangesAsync(cancellationToken);
        return response;
    }

    /// <inheritdoc />
    public async Task<NotifyPaymentQrResponseDto> HandlePaymentNotificationAsync(
        NotifyPaymentQrRequestDto request,
        CancellationToken cancellationToken = default
    )
    {
        var payment = ValidatePaymentNotificationRequest(request);

        using var scope = logger.BeginScope(
            new Dictionary<string, object>
            {
                ["QrId"] = payment.QrId,
                ["TransactionId"] = payment.TransactionId,
                ["Amount"] = payment.Amount,
                ["Currency"] = payment.Currency,
                ["BranchCode"] = payment.BranchCode
            }
        );

        logger.LogInformation("Notificación de pago QR recibida desde Banco Económico.");

        var pagoQr = await dbContext.PagosQr.SingleOrDefaultAsync(x => x.QrId == payment.QrId.Trim(), cancellationToken);

        if (pagoQr is null)
        {
            throw new ArgumentException("No existe un QR registrado para payment.qrId.");
        }

        if (!string.Equals(pagoQr.TransactionId, payment.TransactionId.Trim(), StringComparison.Ordinal))
        {
            throw new ArgumentException("payment.transactionId no coincide con el QR registrado.");
        }

        if (pagoQr.Status == PagoQrStatus.Pendiente)
        {
            pagoQr.MarkAsPaid(ParsePaymentDateTimeUtc(payment.PaymentDate, payment.PaymentTime));
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return new NotifyPaymentQrResponseDto
        {
            ResponseCode = 0,
            Message = string.Empty
        };
    }

    private static void ValidateGenerateQrRequest(GenerateQrRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.TransactionId))
        {
            throw new ArgumentException("transactionId es requerido.");
        }

        if (request.Amount <= 0)
        {
            throw new ArgumentException("amount debe ser mayor a cero.");
        }

        var currency = request.Currency?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(currency) || !ValidCurrencies.Contains(currency))
        {
            throw new ArgumentException("currency debe ser BOB o USD.");
        }

        request.Currency = currency;

        if (!DateOnly.TryParse(request.DueDate, out _))
        {
            throw new ArgumentException("dueDate no tiene un formato válido.");
        }

        if (request.BranchCode?.Length > 5)
        {
            throw new ArgumentException("branchCode no puede exceder 5 caracteres.");
        }
    }

    private static NotifyPaymentQrRequestDto.PaymentDto ValidatePaymentNotificationRequest(
        NotifyPaymentQrRequestDto request
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Payment is null)
        {
            throw new ArgumentException("payment es requerido.");
        }

        var payment = request.Payment;

        if (string.IsNullOrWhiteSpace(payment.QrId))
        {
            throw new ArgumentException("payment.qrId es requerido.");
        }

        if (string.IsNullOrWhiteSpace(payment.TransactionId))
        {
            throw new ArgumentException("payment.transactionId es requerido.");
        }

        if (!DateOnly.TryParse(payment.PaymentDate, out _))
        {
            throw new ArgumentException("payment.paymentDate no tiene un formato válido.");
        }

        if (!TimeOnly.TryParse(payment.PaymentTime, out _))
        {
            throw new ArgumentException("payment.paymentTime no tiene un formato válido.");
        }

        var normalizedCurrency = payment.Currency?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalizedCurrency) || !ValidCurrencies.Contains(normalizedCurrency))
        {
            throw new ArgumentException("payment.currency debe ser BOB o USD.");
        }

        payment.Currency = normalizedCurrency;

        if (payment.Amount <= 0)
        {
            throw new ArgumentException("payment.amount debe ser mayor a cero.");
        }

        if (string.IsNullOrWhiteSpace(payment.SenderBankCode))
        {
            throw new ArgumentException("payment.senderBankCode es requerido.");
        }

        if (string.IsNullOrWhiteSpace(payment.SenderName))
        {
            throw new ArgumentException("payment.senderName es requerido.");
        }

        if (string.IsNullOrWhiteSpace(payment.SenderAccount))
        {
            throw new ArgumentException("payment.senderAccount es requerido.");
        }

        if (string.IsNullOrWhiteSpace(payment.Description))
        {
            throw new ArgumentException("payment.description es requerido.");
        }

        if (string.IsNullOrWhiteSpace(payment.BranchCode))
        {
            throw new ArgumentException("payment.branchCode es requerido.");
        }

        return payment;
    }

    private static DateTime ParsePaymentDateTimeUtc(string paymentDate, string paymentTime)
    {
        var date = DateOnly.Parse(paymentDate);
        var time = TimeOnly.Parse(paymentTime);
        var localPaymentDateTime = date.ToDateTime(time, DateTimeKind.Unspecified);

        // Banco Económico reporta la fecha y hora local de Bolivia (UTC-04:00).
        return new DateTimeOffset(localPaymentDateTime, TimeSpan.FromHours(-4)).UtcDateTime;
    }
}
