using Application.DTOs.BancoEconomico.Requests;
using Application.DTOs.BancoEconomico.Responses;
using Application.Interfaces.Internal;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BancoEconomicoController : ControllerBase
{
    private readonly IBancoEconomicoService _bancoEconomicoService;

    public BancoEconomicoController(IBancoEconomicoService bancoEconomicoService)
    {
        _bancoEconomicoService = bancoEconomicoService;
    }

    /// <summary>
    /// Genera un código QR en Banco Económico.
    /// </summary>
    [HttpPost("generate-qr")]
    [ProducesResponseType(typeof(GenerateQrResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GenerateQr(
        [FromBody] GenerateQrRequestDto request,
        CancellationToken cancellationToken)
    {
        var response = await _bancoEconomicoService.GenerateQrAsync(request, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Anula el QR pendiente asociado a un pago de deudas de Cospail.
    /// </summary>
    [HttpPost("annul-qr")]
    [ProducesResponseType(typeof(AnnulQrResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> AnnulQr(
        [FromBody] AnnulQrRequestDto request,
        CancellationToken cancellationToken)
    {
        var response = await _bancoEconomicoService.AnnulQrAsync(request, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Verifica el estado actual de un código QR en Banco Económico (7.4).
    /// </summary>
    [HttpGet("qr-status/{qrId}")]
    [ProducesResponseType(typeof(QrStatusResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetQrStatus(
        [FromRoute] string qrId,
        CancellationToken cancellationToken)
    {
        var response = await _bancoEconomicoService.GetQrStatusAsync(
            new QrStatusRequestDto { QrId = qrId },
            cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Lista los QR pagados en una fecha en Banco Económico (7.6). Fecha con formato yyyyMMdd.
    /// </summary>
    [HttpGet("paid-qr/{fecha}")]
    [ProducesResponseType(typeof(PaidQrListResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaidQrList(
        [FromRoute] string fecha,
        CancellationToken cancellationToken)
    {
        var response = await _bancoEconomicoService.GetPaidQrListAsync(
            new PaidQrListRequestDto { Fecha = fecha },
            cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Consulta los movimientos de la cuenta configurada por período (8.1).
    /// </summary>
    [HttpPost("query-movements")]
    [ProducesResponseType(typeof(QueryMovementsResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> QueryMovements(
        [FromBody] QueryMovementsRequestDto request,
        CancellationToken cancellationToken)
    {
        var response = await _bancoEconomicoService.QueryMovementsAsync(request, cancellationToken);
        return Ok(response);
    }
}
