using Application.DTOs.BancoEconomico.Requests;
using Application.DTOs.BancoEconomico.Responses;
using Application.Interfaces.External;
using Application.Interfaces.Internal;
using Microsoft.Extensions.Logging;

namespace Application.Services;

/// <summary>
/// Servicio de aplicación para operaciones con Banco Económico.
/// </summary>
public sealed class BancoEconomicoService : IBancoEconomicoService
{
    private static readonly string[] ValidCurrencies = ["BOB", "USD"];
    private readonly IBancoEconomicoQrClient _bancoEconomicoQrClient;
    private readonly ILogger<BancoEconomicoService> _logger;

    public BancoEconomicoService(
        IBancoEconomicoQrClient bancoEconomicoQrClient,
        ILogger<BancoEconomicoService> logger
    )
    {
        _bancoEconomicoQrClient = bancoEconomicoQrClient;
        _logger = logger;
    }

    /// <summary>
    /// Autentica contra Banco Económico.
    /// </summary>
    public async Task<AuthenticateResponseDto> AuthenticateAsync(
        CancellationToken cancellationToken = default
    )
    {
        return await _bancoEconomicoQrClient.AuthenticateAsync(cancellationToken);
    }

    /// <summary>
    /// Genera un código QR en Banco Económico.
    /// </summary>
    public async Task<GenerateQrResponseDto> GenerateQrAsync(
        GenerateQrRequestDto request,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogInformation(
            "Solicitando generación de QR. TransactionId: {TransactionId}",
            request.TransactionId
        );

        var auth = await _bancoEconomicoQrClient.AuthenticateAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(auth.Token))
        {
            throw new InvalidOperationException(
                "No se recibió token de autenticación desde Banco Económico."
            );
        }

        var response = await _bancoEconomicoQrClient.GenerateQrAsync(
            auth.Token,
            request,
            cancellationToken
        );

        return response;
    }

    /// <summary>
    /// Procesa la notificación del pago de un QR recibida desde Banco Económico.
    /// </summary>
    public Task<NotifyPaymentQrResponseDto> HandlePaymentNotificationAsync(
        NotifyPaymentQrRequestDto request,
        CancellationToken cancellationToken = default
    )
    {
        var payment = ValidatePaymentNotificationRequest(request);

        using var scope = _logger.BeginScope(
            new Dictionary<string, object>
            {
                ["QrId"] = payment.QrId,
                ["TransactionId"] = payment.TransactionId,
                ["Amount"] = payment.Amount,
                ["Currency"] = payment.Currency,
                ["BranchCode"] = payment.BranchCode
            }
        );

        _logger.LogInformation(
            "Notificación de pago QR recibida desde Banco Económico. SenderBankCode: {SenderBankCode}, SenderName: {SenderName}, SenderAccount: {SenderAccount}",
            payment.SenderBankCode,
            payment.SenderName,
            payment.SenderAccount
        );

        // Punto de extensión para fase 2: mapear esta notificación a un comando interno
        // y confirmar el pago en Cospail con idempotencia/persistencia.
        var response = new NotifyPaymentQrResponseDto
        {
            ResponseCode = 0,
            Message = string.Empty
        };

        return Task.FromResult(response);
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

        if (string.IsNullOrWhiteSpace(payment.PaymentDate))
        {
            throw new ArgumentException("payment.paymentDate es requerido.");
        }

        if (!DateTime.TryParse(payment.PaymentDate, out _))
        {
            throw new ArgumentException("payment.paymentDate no tiene un formato válido.");
        }

        if (string.IsNullOrWhiteSpace(payment.PaymentTime))
        {
            throw new ArgumentException("payment.paymentTime es requerido.");
        }

        if (!TimeOnly.TryParse(payment.PaymentTime, out _))
        {
            throw new ArgumentException("payment.paymentTime no tiene un formato válido.");
        }

        if (string.IsNullOrWhiteSpace(payment.Currency))
        {
            throw new ArgumentException("payment.currency es requerido.");
        }

        var normalizedCurrency = payment.Currency.Trim().ToUpperInvariant();

        if (!ValidCurrencies.Contains(normalizedCurrency))
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
}
