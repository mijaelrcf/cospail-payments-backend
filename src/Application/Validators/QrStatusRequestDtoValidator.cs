using Application.DTOs.BancoEconomico.Requests;
using FluentValidation;

namespace Application.Validators;

/// <summary>
/// Valida la solicitud de verificación de estado de QR (7.4).
/// </summary>
public sealed class QrStatusRequestDtoValidator : AbstractValidator<QrStatusRequestDto>
{
    public QrStatusRequestDtoValidator()
    {
        RuleFor(x => x.QrId)
            .NotEmpty()
            .WithMessage("qrId es requerido.");
    }
}
