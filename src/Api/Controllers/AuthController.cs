using Application.DTOs.Admin.Requests;
using Application.DTOs.Admin.Responses;
using Application.Interfaces.Internal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Api.Controllers;

/// <summary>
/// Autenticación del panel de administración.
/// </summary>
[ApiController]
[Route("api/admin/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    /// <summary>
    /// Inicia sesión en el panel de administración y devuelve un token JWT.
    /// </summary>
    /// <param name="request">Credenciales de inicio de sesión.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("AdminLogin")]
    [ProducesResponseType(typeof(AuthLoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login(
        [FromBody] AuthLoginRequestDto request,
        CancellationToken cancellationToken
    )
    {
        var result = await authService.LoginAsync(request, cancellationToken);

        return Ok(result);
    }
}
