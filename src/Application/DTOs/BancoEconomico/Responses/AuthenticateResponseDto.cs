using System.Text.Json.Serialization;

namespace Application.DTOs.BancoEconomico.Responses;

/// <summary>
/// Response de autenticación de Banco Económico.
/// </summary>
public sealed class AuthenticateResponseDto
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    [JsonPropertyName("responseCode")]
    public int ResponseCode { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}