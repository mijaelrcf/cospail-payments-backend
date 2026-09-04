using Application.DTOs.Cospail.Requests;
using Application.DTOs.Cospail.Responses;
using Application.Interfaces.Internal;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CospailController : ControllerBase
{
    private readonly ICospailService _cospailService;

    public CospailController(ICospailService cospailService)
    {
        _cospailService = cospailService;
    }

    /// <summary>
    /// Consulta la deuda de un socio mediante código fijo y documento de identidad o NIT.
    /// </summary>
    /// <param name="fixedCode">Código fijo del socio.</param>
    /// <param name="documentId">Documento de Identidad o NIT.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpGet("member-debt-by-document")]
    [ProducesResponseType(typeof(GetMemberDebtByDocumentResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMemberDebtByDocument(
        [FromQuery] int fixedCode,
        [FromQuery] string documentId,
        CancellationToken cancellationToken
    )
    {
        if (fixedCode <= 0)
        {
            return BadRequest("fixedCode debe ser mayor a cero.");
        }

        if (string.IsNullOrWhiteSpace(documentId))
        {
            return BadRequest("documentId es requerido.");
        }

        var result = await _cospailService.GetMemberDebtByDocumentAsync(
            fixedCode,
            documentId,
            cancellationToken
        );

        return Ok(result);
    }

    /// <summary>
    /// Confirma un pago realizado por un socio ante Cospail.
    /// </summary>
    /// <param name="request">Datos del pago a confirmar.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpPost("payments/confirm")]
    [ProducesResponseType(typeof(ConfirmPaymentResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ConfirmPayment(
        [FromBody] ConfirmPaymentRequestDto request,
        CancellationToken cancellationToken
    )
    {
        var result = await _cospailService.ConfirmPaymentAsync(request, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Valida y persiste un pago agrupado de una o más deudas de Cospail.
    /// </summary>
    /// <param name="request">Deudas seleccionadas para el pago.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpPost("payments/initiate")]
    [ProducesResponseType(typeof(PagoCospailResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> InitiatePayment(
        [FromBody] InitiatePaymentRequestDto request,
        CancellationToken cancellationToken
    )
    {
        var result = await _cospailService.InitiatePaymentAsync(request, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Consulta el estado actual de un pago agrupado de deudas de Cospail.
    /// </summary>
    /// <param name="pagoCospailId">Identificador del pago.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpGet("payments/{pagoCospailId:guid}")]
    [ProducesResponseType(typeof(PagoCospailResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaymentStatus(
        [FromRoute] Guid pagoCospailId,
        CancellationToken cancellationToken
    )
    {
        var result = await _cospailService.GetPaymentStatusAsync(pagoCospailId, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Devuelve el QR vigente (pendiente y no vencido) del socio, para mostrarlo
    /// hasta que se pague o se anule.
    /// </summary>
    /// <param name="fixedCode">Código fijo del socio.</param>
    /// <param name="documentId">Documento de Identidad o NIT.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpGet("payments/active-qr")]
    [ProducesResponseType(typeof(ActiveQrResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveQr(
        [FromQuery] int fixedCode,
        [FromQuery] string documentId,
        CancellationToken cancellationToken
    )
    {
        if (fixedCode <= 0)
        {
            return BadRequest("fixedCode debe ser mayor a cero.");
        }

        if (string.IsNullOrWhiteSpace(documentId))
        {
            return BadRequest("documentId es requerido.");
        }

        var result = await _cospailService.GetActiveQrAsync(
            fixedCode,
            documentId,
            cancellationToken
        );

        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    /// <summary>
    /// Devuelve las últimas 6 facturas del socio (últimos 6 meses) desde Cospail.
    /// El rango de fechas se calcula en el servidor.
    /// </summary>
    /// <param name="fixedCode">Código fijo del socio.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpGet("invoices/last-6-months")]
    [ProducesResponseType(typeof(List<InvoiceSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLast6MonthsInvoices(
        [FromQuery] int fixedCode,
        CancellationToken cancellationToken = default
    )
    {
        if (fixedCode <= 0)
        {
            return BadRequest("fixedCode debe ser mayor a cero.");
        }

        var result = await _cospailService.GetLast6MonthsInvoicesAsync(
            fixedCode,
            cancellationToken
        );

        return Ok(result);
    }

    /// <summary>
    /// Devuelve el PDF (Base64) de una factura por número de crédito.
    /// </summary>
    /// <param name="creditNumber">Número de crédito de la factura.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpGet("invoices/{creditNumber:int}/pdf")]
    [ProducesResponseType(typeof(InvoicePdfDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInvoicePdf(
        [FromRoute] int creditNumber,
        CancellationToken cancellationToken = default
    )
    {
        if (creditNumber <= 0)
        {
            return BadRequest("creditNumber debe ser mayor a cero.");
        }

        var result = await _cospailService.GetInvoicePdfAsync(
            creditNumber,
            cancellationToken
        );

        return Ok(result);
    }

    /// <summary>
    /// Devuelve los últimos 5 pagos de Cospail del socio.
    /// </summary>
    /// <param name="fixedCode">Código fijo del socio.</param>
    /// <param name="status">Estado del pago (predeterminado: CospailRegistrado).</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpGet("payments/recent")]
    [ProducesResponseType(typeof(List<RecentPaymentItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecentPayments(
        [FromQuery] int fixedCode,
        [FromQuery] PagoCospailStatus status = PagoCospailStatus.CospailRegistrado,
        CancellationToken cancellationToken = default
    )
    {
        if (fixedCode <= 0)
        {
            return BadRequest("fixedCode debe ser mayor a cero.");
        }

        var result = await _cospailService.GetRecentPaymentsAsync(
            fixedCode,
            status,
            cancellationToken
        );

        return Ok(result);
    }
}
