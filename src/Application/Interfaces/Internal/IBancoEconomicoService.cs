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

    /// <summary>
    /// Consulta el estado actual de un código QR en Banco Económico (7.4).
    /// Proxy puro: no modifica la base de datos local.
    /// </summary>
    Task<QrStatusResponseDto> GetQrStatusAsync(
        QrStatusRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retorna el listado de QR pagados en una fecha en Banco Económico (7.6).
    /// Proxy puro: no modifica la base de datos local.
    /// </summary>
    Task<PaidQrListResponseDto> GetPaidQrListAsync(
        PaidQrListRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Consulta los movimientos de la cuenta configurada por período (8.1).
    /// Proxy puro: no modifica la base de datos local.
    /// </summary>
    Task<QueryMovementsResponseDto> QueryMovementsAsync(
        QueryMovementsRequestDto request,
        CancellationToken cancellationToken = default);
}
