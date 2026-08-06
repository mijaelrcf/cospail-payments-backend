using Application.DTOs.BancoEconomico.Requests;
using Application.DTOs.BancoEconomico.Responses;
using Application.Interfaces.Internal;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/qrsimple/notifyPaymentQR")]
public sealed class NotifyPaymentQrController : ControllerBase
{
    private readonly IBancoEconomicoService _bancoEconomicoService;
    private readonly ILogger<NotifyPaymentQrController> _logger;

    public NotifyPaymentQrController(
        IBancoEconomicoService bancoEconomicoService,
        ILogger<NotifyPaymentQrController> logger
    )
    {
        _bancoEconomicoService = bancoEconomicoService;
        _logger = logger;
    }

    /// <summary>
    /// Recibe la notificación de pago de un QR enviada por Banco Económico.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(NotifyPaymentQrResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> NotifyPaymentQr(
        [FromBody] NotifyPaymentQrRequestDto request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var response = await _bancoEconomicoService.HandlePaymentNotificationAsync(
                request,
                cancellationToken
            );

            return Ok(response);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Banco Económico envió una notificación QR inválida.");
            return Ok(new NotifyPaymentQrResponseDto
            {
                ResponseCode = 1,
                Message = ex.Errors.FirstOrDefault()?.ErrorMessage ?? "Solicitud inválida."
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Banco Económico envió una notificación QR inválida.");
            return Ok(new NotifyPaymentQrResponseDto
            {
                ResponseCode = 1,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error procesando la notificación QR de Banco Económico.");
            return Ok(new NotifyPaymentQrResponseDto
            {
                ResponseCode = 99,
                Message = "Ocurrió un error procesando la notificación."
            });
        }
    }
}
