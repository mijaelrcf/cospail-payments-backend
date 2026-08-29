namespace Application.DTOs.Admin.Responses;

/// <summary>
/// Resultado de un inicio de sesión correcto en el panel de administración.
/// </summary>
public sealed class AuthLoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}
