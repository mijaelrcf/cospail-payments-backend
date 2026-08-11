using Application.DTOs.Cospail.Requests;
using FluentValidation;

namespace Application.Validators;

/// <summary>
/// Valida la solicitud de inicio de un pago agrupado de deudas de Cospail.
/// </summary>
public sealed class InitiatePaymentRequestDtoValidator : AbstractValidator<InitiatePaymentRequestDto>
{
    public InitiatePaymentRequestDtoValidator()
    {
        RuleFor(x => x.FixedCode)
            .GreaterThan(0)
            .WithMessage("fixedCode debe ser mayor a cero.");

        RuleFor(x => x.DocumentId)
            .NotEmpty()
            .WithMessage("documentId es requerido.");

        RuleFor(x => x.Debts)
            .NotEmpty()
            .WithMessage("debe seleccionar al menos una deuda.");

        RuleForEach(x => x.Debts).SetValidator(new InitiatePaymentDebtValidator());
    }

    private sealed class InitiatePaymentDebtValidator : AbstractValidator<InitiatePaymentDebtDto>
    {
        public InitiatePaymentDebtValidator()
        {
            RuleFor(x => x.CreditNumber)
                .GreaterThan(0)
                .WithMessage("creditNumber debe ser mayor a cero.");

            RuleFor(x => x.Type)
                .GreaterThan(0)
                .WithMessage("type debe ser mayor a cero.");

            RuleFor(x => x.Amount)
                .GreaterThan(0)
                .WithMessage("amount debe ser mayor a cero.");
        }
    }
}