using Application.DTOs.BancoEconomico.Requests;
using FluentValidation;

namespace Application.Validators;

/// <summary>
/// Valida la solicitud de anulación de un QR de Banco Económico.
/// </summary>
public sealed class AnnulQrRequestDtoValidator : AbstractValidator<AnnulQrRequestDto>
{
    public AnnulQrRequestDtoValidator()
    {
        RuleFor(x => x.PagoCospailId)
            .NotEmpty()
            .WithMessage("pagoCospailId es requerido.");
    }
}
