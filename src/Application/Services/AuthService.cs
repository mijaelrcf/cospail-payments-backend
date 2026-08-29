using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Application.Auth;
using Application.DTOs.Admin.Requests;
using Application.DTOs.Admin.Responses;
using Application.Interfaces.Internal;
using Application.Options;
using FluentValidation;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Application.Services;

/// <summary>
/// Servicio de aplicación que autentica a los usuarios del panel de administración
/// y emite tokens JWT.
/// </summary>
public sealed class AuthService(
    IOptions<AuthOptions> authOptions,
    IValidator<AuthLoginRequestDto> loginValidator
) : IAuthService
{
    public Task<AuthLoginResponseDto> LoginAsync(
        AuthLoginRequestDto request,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        loginValidator.ValidateAndThrow(request);

        var user = authOptions
            .Value.Users.FirstOrDefault(u =>
                string.Equals(u.Username, request.Username, StringComparison.OrdinalIgnoreCase)
            );

        if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Credenciales inválidas.");
        }

        var options = authOptions.Value;
        var expiresAt = DateTime.UtcNow.AddMinutes(options.TokenLifetimeMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Username),
            new(JwtRegisteredClaimNames.Name, user.DisplayName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Role, "Admin")
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt,
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return Task.FromResult(
            new AuthLoginResponseDto
            {
                Token = tokenString,
                ExpiresAt = expiresAt,
                DisplayName = user.DisplayName
            }
        );
    }
}
