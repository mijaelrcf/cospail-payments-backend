using System.Globalization;
using Application.DTOs.BancoEconomico.Requests;
using FluentValidation;

namespace Application.Validators;

/// <summary>
/// Valida la notificación de pago QR enviada por Banco Económico.
/// </summary>
public sealed class NotifyPaymentQrRequestDtoValidator : AbstractValidator<NotifyPaymentQrRequestDto>
{
    public NotifyPaymentQrRequestDtoValidator()
    {
        RuleFor(x => x.Payment)
            .NotNull()
            .WithMessage("payment es requerido.")
            .SetValidator(new PaymentValidator()!);
    }

    private sealed class PaymentValidator : AbstractValidator<NotifyPaymentQrRequestDto.PaymentDto>
    {
        public PaymentValidator()
        {
            RuleFor(x => x.QrId)
                .NotEmpty()
                .WithMessage("payment.qrId es requerido.");

            RuleFor(x => x.TransactionId)
                .NotEmpty()
                .WithMessage("payment.transactionId es requerido.");

            RuleFor(x => x.PaymentDate)
                .NotEmpty()
                .WithMessage("payment.paymentDate es requerido.")
                .Must(BeValidPaymentDate)
                .WithMessage("payment.paymentDate debe tener formato yyyy-MM-dd o yyyy-MM-ddTHH:mm:ss.");

            RuleFor(x => x.PaymentTime)
                .NotEmpty()
                .WithMessage("payment.paymentTime es requerido.")
                .Must(t => TimeOnly.TryParseExact(
                    t,
                    "HH:mm:ss",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out _))
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

            RuleFor(x => x.Description)
                .NotEmpty()
                .WithMessage("payment.description es requerido.");
        }
    }

    private static bool BeValidPaymentDate(string value)
    {
        if (DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
        {
            return true;
        }

        return DateTime.TryParseExact(
            value,
            "yyyy-MM-ddTHH:mm:ss",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out _
        );
    }
}
