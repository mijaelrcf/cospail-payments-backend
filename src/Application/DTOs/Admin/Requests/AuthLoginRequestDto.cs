namespace Application.DTOs.Admin.Requests;

/// <summary>
/// Credenciales de inicio de sesión del panel de administración.
/// </summary>
public sealed class AuthLoginRequestDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
