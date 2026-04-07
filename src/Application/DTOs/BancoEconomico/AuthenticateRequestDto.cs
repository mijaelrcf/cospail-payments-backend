namespace Application.DTOs.BancoEconomico;

/// <summary>
/// Request para autenticación en Banco Económico.
/// </summary>
public sealed class AuthenticateRequestDto
{
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}