using Application.DTOs.Cospail.Requests;
using FluentValidation;

namespace Application.Validators;

/// <summary>
/// Valida la solicitud de confirmación y registro de cobro en Cospail.
/// </summary>
public sealed class ConfirmPaymentRequestDtoValidator : AbstractValidator<ConfirmPaymentRequestDto>
{
    public ConfirmPaymentRequestDtoValidator()
    {
        RuleFor(x => x.FixedCode).FixedCode();

        RuleFor(x => x.DocumentId).DocumentId();

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("amount debe ser mayor a cero.");
    }
}
