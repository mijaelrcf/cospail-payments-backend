using Application.DTOs.Admin.Requests;
using Application.DTOs.Admin.Responses;

namespace Application.Interfaces.Internal;

/// <summary>
/// Servicio de aplicación para la autenticación del panel de administración.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Valida las credenciales contra los usuarios configurados y, si son
    /// correctas, emite un token JWT.
    /// </summary>
    /// <param name="request">Credenciales de inicio de sesión.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <exception cref="UnauthorizedAccessException">Cuando las credenciales no son válidas.</exception>
    Task<AuthLoginResponseDto> LoginAsync(
        AuthLoginRequestDto request,
        CancellationToken cancellationToken = default
    );
}
