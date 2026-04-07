using Application.DTOs.BancoEconomico;

namespace Application.Interfaces.External;

/// <summary>
/// Cliente externo para integración con Banco Económico.
/// </summary>
public interface IBancoEconomicoQrClient
{
    /// <summary>
    /// Autentica contra Banco Económico y devuelve el token de acceso.
    /// </summary>
    Task<AuthenticateResponseDto> AuthenticateAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Genera un código QR para pago utilizando el token de acceso obtenido previamente.
    /// </summary>
    Task<GenerateQrResponseDto> GenerateQrAsync(
        string bearerToken,
        GenerateQrRequestDto request,
        CancellationToken cancellationToken = default);
}