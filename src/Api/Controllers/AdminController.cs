using Application.DTOs.Admin.Requests;
using Application.DTOs.Admin.Responses;
using Application.Interfaces.Internal;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// Reportes del panel de administración (requiere rol Admin).
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController(IAdminReportService adminReportService) : ControllerBase
{
    /// <summary>
    /// Devuelve un reporte paginado de pagos de Cospail con sus deudas.
    /// </summary>
    /// <param name="from">Fecha de inicio del rango (opcional, sobre CreatedAtUtc).</param>
    /// <param name="to">Fecha de fin del rango (opcional, inclusiva hasta fin de día).</param>
    /// <param name="status">Estado del pago (opcional; si se omite se devuelven todos los estados).</param>
    /// <param name="fixedCode">Código fijo del socio (opcional).</param>
    /// <param name="documentId">Documento de identidad o NIT del socio (opcional, búsqueda parcial).</param>
    /// <param name="page">Página solicitada, desde 1.</param>
    /// <param name="pageSize">Tamaño de página (máx. 100).</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpGet("payments/report")]
    [ProducesResponseType(typeof(AdminPaymentReportResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaymentReport(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] PagoCospailStatus? status = null,
        [FromQuery] int? fixedCode = null,
        [FromQuery] string? documentId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default
    )
    {
        if (from.HasValue && to.HasValue && from.Value > to.Value)
        {
            return BadRequest("La fecha de inicio no puede ser posterior a la fecha de fin.");
        }

        if (fixedCode.HasValue && fixedCode.Value <= 0)
        {
            return BadRequest("fixedCode debe ser mayor a cero.");
        }

        var request = new AdminPaymentReportRequestDto
        {
            From = from,
            To = to,
            Status = status,
            FixedCode = fixedCode,
            DocumentId = documentId,
            Page = page,
            PageSize = pageSize
        };

        var result = await adminReportService.GetPaymentReportAsync(request, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Devuelve el detalle de un pago con sus deudas y la notificación de pago QR asociada.
    /// </summary>
    /// <param name="pagoCospailId">Identificador del pago.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpGet("payments/{pagoCospailId:guid}")]
    [ProducesResponseType(typeof(AdminPaymentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPaymentDetail(
        [FromRoute] Guid pagoCospailId,
        CancellationToken cancellationToken
    )
    {
        var result = await adminReportService.GetPaymentDetailAsync(pagoCospailId, cancellationToken);

        return Ok(result);
    }
}
