using Application.DTOs.Admin.Requests;
using FluentValidation;

namespace Application.Validators;

/// <summary>
/// Valida las credenciales de inicio de sesión del panel de administración.
/// </summary>
public sealed class AuthLoginRequestDtoValidator : AbstractValidator<AuthLoginRequestDto>
{
    public AuthLoginRequestDtoValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .WithMessage("username es requerido.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("password es requerido.");
    }
}
