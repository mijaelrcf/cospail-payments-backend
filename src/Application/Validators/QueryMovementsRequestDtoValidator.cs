using Application.Common;
using Application.DTOs.BancoEconomico.Requests;
using FluentValidation;
using System.Globalization;

namespace Application.Validators;

/// <summary>
/// Valida la solicitud de consulta de movimientos (8.1).
/// </summary>
public sealed class QueryMovementsRequestDtoValidator : AbstractValidator<QueryMovementsRequestDto>
{
    public QueryMovementsRequestDtoValidator()
    {
        RuleFor(x => x.StartDate)
            .NotEmpty()
            .WithMessage("startDate es requerido.")
            .Must(BeValidDate)
            .WithMessage("startDate debe tener formato yyyy-MM-dd.");

        RuleFor(x => x.EndDate)
            .NotEmpty()
            .WithMessage("endDate es requerido.")
            .Must(BeValidDate)
            .WithMessage("endDate debe tener formato yyyy-MM-dd.");

        RuleFor(x => x)
            .Must(HaveValidRange)
            .WithMessage("startDate no puede ser posterior a endDate.");
    }

    private static bool BeValidDate(string? value) =>
        DateOnly.TryParseExact(
            (value ?? string.Empty).Trim(),
            PaymentDateTime.DateFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out _);

    private static bool HaveValidRange(QueryMovementsRequestDto request)
    {
        if (!BeValidDate(request.StartDate) || !BeValidDate(request.EndDate))
        {
            return true;
        }

        var start = DateOnly.ParseExact(request.StartDate.Trim(), PaymentDateTime.DateFormat, CultureInfo.InvariantCulture);
        var end = DateOnly.ParseExact(request.EndDate.Trim(), PaymentDateTime.DateFormat, CultureInfo.InvariantCulture);

        return start <= end;
    }
}
