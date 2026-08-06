namespace Application.DTOs.BancoEconomico.Responses;

/// <summary>
/// Response de autenticación de Banco Económico.
/// </summary>
public sealed class AuthenticateResponseDto
{
    public string Token { get; set; } = string.Empty;

    public int ResponseCode { get; set; }

    public string Message { get; set; } = string.Empty;
}
