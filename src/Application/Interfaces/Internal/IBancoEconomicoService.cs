using Application.DTOs.BancoEconomico.Requests;
using Application.DTOs.BancoEconomico.Responses;

namespace Application.Interfaces.Internal;

/// <summary>
/// Servicio de aplicación para operaciones de pago.
/// </summary>
public interface IBancoEconomicoService
{
    /// <summary>
    /// Genera el codigo QR.
    /// </summary>
    Task<GenerateQrResponseDto> GenerateQrAsync(
        GenerateQrRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Anula el QR pendiente asociado a un pago de deudas de Cospail.
    /// </summary>
    Task<AnnulQrResponseDto> AnnulQrAsync(
        AnnulQrRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Procesa la notificación de estado de un pago enviada por Banco Económico.
    /// </summary>
    Task<NotifyPaymentQrResponseDto> HandlePaymentNotificationAsync(
        NotifyPaymentQrRequestDto request,
        CancellationToken cancellationToken = default);
}
