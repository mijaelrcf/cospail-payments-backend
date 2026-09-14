using Application.Common;
using FluentValidation;
using static Application.DTOs.BancoEconomico.Requests.NotifyPaymentQrRequestDto;

namespace Application.Validators;

/// <summary>
/// Valida el objeto <c>payment</c> de la notificación de pago QR.
/// </summary>
public sealed class NotifyPaymentQrPaymentValidator : AbstractValidator<PaymentDto>
{
    public NotifyPaymentQrPaymentValidator()
    {
        RuleFor(x => x.QrId)
            .NotEmpty()
            .WithMessage("payment.qrId es requerido.");

        RuleFor(x => x.TransactionId)
            .NotEmpty()
            .WithMessage("payment.transactionId es requerido.");

        // paymentDate es opcional/tolerante: el banco envía "yyyy-MM-dd",
        // "yyyy-MM-ddTHH:mm:ss" o ISO 8601 con zona ("2026-09-07T04:00:00Z").
        // Solo se valida el formato cuando viene con valor.
        RuleFor(x => x.PaymentDate)
            .Must(BeValidPaymentDate)
            .When(x => !string.IsNullOrWhiteSpace(x.PaymentDate))
            .WithMessage("payment.paymentDate debe tener formato yyyy-MM-dd o ISO 8601 (yyyy-MM-ddTHH:mm:ss).");

        // paymentTime es opcional: si viene vacío se usa la hora incluida
        // en paymentDate o medianoche. Cuando viene, debe ser HH:mm:ss.
        RuleFor(x => x.PaymentTime)
            .Must(BeValidPaymentTime)
            .When(x => !string.IsNullOrWhiteSpace(x.PaymentTime))
            .WithMessage("payment.paymentTime debe tener formato HH:mm:ss.");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .WithMessage("payment.currency es requerido.")
            .Must(c => c is "BOB" or "USD")
            .WithMessage("payment.currency debe ser BOB o USD.");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("payment.amount debe ser mayor a cero.");

        RuleFor(x => x.SenderBankCode)
            .NotEmpty()
            .WithMessage("payment.senderBankCode es requerido.");

        RuleFor(x => x.SenderName)
            .NotEmpty()
            .WithMessage("payment.senderName es requerido.");

        RuleFor(x => x.SenderAccount)
            .NotEmpty()
            .WithMessage("payment.senderAccount es requerido.");

        // description es opcional: el banco a veces no la envía o la envía vacía.
        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("payment.description no debe exceder 500 caracteres.");
    }

    private static bool BeValidPaymentDate(string? value)
    {
        var trimmed = (value ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return true;
        }

        return PaymentDateTime.TryParseDate(trimmed, out _);
    }

    private static bool BeValidPaymentTime(string? value)
    {
        var trimmed = (value ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return true;
        }

        return PaymentDateTime.IsValidTimeFormat(trimmed);
    }
}
