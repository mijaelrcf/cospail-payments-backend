using System.Globalization;
using Application.DTOs.BancoEconomico.Requests;
using FluentValidation;

namespace Application.Validators;

/// <summary>
/// Valida la solicitud de generación de QR de Banco Económico.
/// </summary>
public sealed class GenerateQrRequestDtoValidator : AbstractValidator<GenerateQrRequestDto>
{
    public GenerateQrRequestDtoValidator()
    {
        RuleFor(x => x.TransactionId)
            .NotEmpty()
            .WithMessage("transactionId es requerido.")
            .MaximumLength(100)
            .WithMessage("transactionId no puede exceder 100 caracteres.");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("amount debe ser mayor a cero.")
            .When(x => x.PagoCospailId is null);

        RuleFor(x => x.Currency)
            .NotEmpty()
            .WithMessage("currency es requerido.")
            .Must(c => c is "BOB" or "USD")
            .WithMessage("currency debe ser BOB o USD.");

        RuleFor(x => x.DueDate)
            .NotEmpty()
            .WithMessage("dueDate es requerido.")
            .Must(d => DateOnly.TryParseExact(
                d,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out _))
            .WithMessage("dueDate debe tener formato yyyy-MM-dd.");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("description no puede exceder 500 caracteres.");

        RuleFor(x => x.BranchCode)
            .MaximumLength(5)
            .WithMessage("branchCode no puede exceder 5 caracteres.");
    }
}
