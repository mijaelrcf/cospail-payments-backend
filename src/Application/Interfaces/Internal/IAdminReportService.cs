using Application.DTOs.Admin.Requests;
using Application.DTOs.Admin.Responses;

namespace Application.Interfaces.Internal;

/// <summary>
/// Servicio de aplicación con consultas de reporte para el panel de administración.
/// </summary>
public interface IAdminReportService
{
    /// <summary>
    /// Devuelve un reporte paginado de pagos de Cospail con sus deudas, aplicando los
    /// filtros recibidos. El estado predeterminado es <c>CospailRegistrado</c>.
    /// </summary>
    Task<AdminPaymentReportResponseDto> GetPaymentReportAsync(
        AdminPaymentReportRequestDto request,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Devuelve el detalle de un pago con sus deudas y la notificación de pago QR
    /// asociada, si existe.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Cuando el pago no existe.</exception>
    Task<AdminPaymentDetailDto> GetPaymentDetailAsync(
        Guid pagoCospailId,
        CancellationToken cancellationToken = default
    );
}
