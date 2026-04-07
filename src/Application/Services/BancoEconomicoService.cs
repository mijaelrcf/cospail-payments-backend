using Application.DTOs.BancoEconomico;
using Application.Interfaces.External;
using Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace Application.Services;

/// <summary>
/// Servicio de aplicación para operaciones con Banco Económico.
/// </summary>
public sealed class BancoEconomicoService : IBancoEconomicoService
{
    private readonly IBancoEconomicoQrClient _bancoEconomicoQrClient;
    private readonly ILogger<BancoEconomicoService> _logger;

    public BancoEconomicoService(
        IBancoEconomicoQrClient bancoEconomicoQrClient,
        ILogger<BancoEconomicoService> logger)
    {
        _bancoEconomicoQrClient = bancoEconomicoQrClient;
        _logger = logger;
    }

    /// <summary>
    /// Autentica contra Banco Económico.
    /// </summary>
    public async Task<AuthenticateResponseDto> AuthenticateAsync(
        CancellationToken cancellationToken = default)
    {
        return await _bancoEconomicoQrClient.AuthenticateAsync(cancellationToken);
    }

    /// <summary>
    /// Genera un código QR en Banco Económico.
    /// </summary>
    public async Task<GenerateQrResponseDto> GenerateQrAsync(
        GenerateQrRequestDto request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Solicitando generación de QR. TransactionId: {TransactionId}",
            request.TransactionId);

        var auth = await _bancoEconomicoQrClient.AuthenticateAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(auth.Token))
        {
            throw new InvalidOperationException(
                "No se recibió token de autenticación desde Banco Económico.");
        }

        var response = await _bancoEconomicoQrClient.GenerateQrAsync(
            auth.Token,
            request,
            cancellationToken);

        return response;
    }
}