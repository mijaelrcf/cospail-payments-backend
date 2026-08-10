using Application.DTOs.BancoEconomico.Requests;
using Application.DTOs.BancoEconomico.Responses;
using Application.Interfaces.External;
using Application.Interfaces.Internal;
using Application.Interfaces.Persistence;
using Domain.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace Application.Services;

/// <summary>
/// Servicio de aplicación para operaciones con Banco Económico.
/// </summary>
public sealed class BancoEconomicoService(
    IBancoEconomicoQrClient bancoEconomicoQrClient,
    IPaymentsDbContext dbContext,
    IValidator<GenerateQrRequestDto> generateQrValidator,
    IValidator<NotifyPaymentQrRequestDto> notifyPaymentValidator,
    ILogger<BancoEconomicoService> logger
) : IBancoEconomicoService
{
    /// <inheritdoc />
    public async Task<GenerateQrResponseDto> GenerateQrAsync(
        GenerateQrRequestDto request,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        request.TransactionId = request.TransactionId.Trim();
        request.Currency = request.Currency?.Trim().ToUpperInvariant() ?? string.Empty;

        await generateQrValidator.ValidateAndThrowAsync(request, cancellationToken);

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

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            throw new ArgumentException("Ya existe un QR registrado para el transactionId proporcionado.");
        }

        return response;
    }

    /// <inheritdoc />
    public async Task<NotifyPaymentQrResponseDto> HandlePaymentNotificationAsync(
        NotifyPaymentQrRequestDto request,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Payment is not null)
        {
            request.Payment.Currency = request.Payment.Currency?.Trim().ToUpperInvariant() ?? string.Empty;
        }

        await notifyPaymentValidator.ValidateAndThrowAsync(request, cancellationToken);

        var payment = request.Payment!;

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

        if (!pagoQr.ModifyAmount && payment.Amount != pagoQr.Amount)
        {
            throw new ArgumentException("payment.amount no coincide con el importe del QR.");
        }

        if (!string.Equals(pagoQr.Currency, payment.Currency, StringComparison.Ordinal))
        {
            throw new ArgumentException("payment.currency no coincide con la moneda del QR.");
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

    private static DateTime ParsePaymentDateTimeUtc(string paymentDate, string paymentTime)
    {
        var date = ParsePaymentDate(paymentDate);
        var time = TimeOnly.Parse(paymentTime);
        var localPaymentDateTime = date.ToDateTime(time, DateTimeKind.Unspecified);

        // Banco Económico reporta la fecha y hora local de Bolivia (UTC-04:00).
        return new DateTimeOffset(localPaymentDateTime, TimeSpan.FromHours(-4)).UtcDateTime;
    }

    private static DateOnly ParsePaymentDate(string paymentDate)
    {
        if (DateOnly.TryParseExact(paymentDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return date;
        }

        var paymentDateTime = DateTime.ParseExact(
            paymentDate,
            "yyyy-MM-ddTHH:mm:ss",
            CultureInfo.InvariantCulture
        );

        return DateOnly.FromDateTime(paymentDateTime);
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
    {
        for (var current = exception.InnerException; current is not null; current = current.InnerException)
        {
            if (
                current.Message.Contains("23505", StringComparison.Ordinal)
                || current.Message.Contains("duplicate key value", StringComparison.OrdinalIgnoreCase)
            )
            {
                return true;
            }
        }

        return false;
    }
}
