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
            .SetValidator(new NotifyPaymentQrPaymentValidator()!);
    }
}
