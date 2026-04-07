using Application.DTOs.BancoEconomico;
using Application.Interfaces.External;
using Application.Interfaces.Services;
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
    /// Endpoint temporal para probar autenticación con Banco Económico.
    /// </summary>
    [HttpPost("authenticate")]
    public async Task<IActionResult> AuthenticateBancoEconomico(CancellationToken cancellationToken)
    {
        var result = await _bancoEconomicoService.AuthenticateAsync(cancellationToken);
        return Ok(result);
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
}
