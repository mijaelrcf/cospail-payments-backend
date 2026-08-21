using Application.DTOs.BancoEconomico.Requests;
using FluentValidation;

namespace Application.Validators;

/// <summary>
/// Valida la solicitud de generación de QR de Banco Económico.
/// El resto de los datos del cobro se resuelven en el servidor a partir del pago.
/// </summary>
public sealed class GenerateQrRequestDtoValidator : AbstractValidator<GenerateQrRequestDto>
{
    public GenerateQrRequestDtoValidator()
    {
        RuleFor(x => x.PagoCospailId)
            .NotEmpty()
            .WithMessage("pagoCospailId es requerido.");

        RuleFor(x => x.BranchCode)
            .MaximumLength(5)
            .WithMessage("branchCode no puede exceder 5 caracteres.");
    }
}
