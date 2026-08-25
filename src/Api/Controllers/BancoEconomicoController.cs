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
}
