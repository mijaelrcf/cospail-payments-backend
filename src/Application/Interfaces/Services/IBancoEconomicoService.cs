
using Application.DTOs.BancoEconomico;

namespace Application.Interfaces.Services;

/// <summary>
/// Servicio de aplicación para operaciones de pago.
/// </summary>
public interface IBancoEconomicoService
{
    /// <summary>
    /// Autentica contra Banco Económico y devuelve el token.
    /// </summary>
    Task<AuthenticateResponseDto> AuthenticateAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Genera el codigo QR.
    /// </summary>
    Task<GenerateQrResponseDto> GenerateQrAsync(
        GenerateQrRequestDto request,
        CancellationToken cancellationToken = default);
}