using Application.Common;
using Application.DTOs.BancoEconomico.Requests;
using FluentValidation;
using System.Globalization;

namespace Application.Validators;

/// <summary>
/// Valida la solicitud del listado de QR pagados (7.6).
/// </summary>
public sealed class PaidQrListRequestDtoValidator : AbstractValidator<PaidQrListRequestDto>
{
    public PaidQrListRequestDtoValidator()
    {
        RuleFor(x => x.Fecha)
            .NotEmpty()
            .WithMessage("fecha es requerida.")
            .Matches(@"^\d{8}$")
            .WithMessage("fecha debe tener formato yyyyMMdd.")
            .Must(BeValidDate)
            .WithMessage("fecha debe ser una fecha válida.")
            .Must(NotBeFutureDate)
            .WithMessage("fecha no puede ser futura.");
    }

    private static bool BeValidDate(string fecha) =>
        DateOnly.TryParseExact(
            (fecha ?? string.Empty).Trim(),
            PaymentDateTime.CompactDateFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out _);

    private static bool NotBeFutureDate(string fecha)
    {
        if (!DateOnly.TryParseExact(
            (fecha ?? string.Empty).Trim(),
            PaymentDateTime.CompactDateFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var date))
        {
            return true;
        }

        return date <= DateOnly.FromDateTime(DateTime.UtcNow);
    }
}
