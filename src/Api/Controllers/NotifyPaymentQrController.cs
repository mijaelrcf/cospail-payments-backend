using System.Text.Json;
using Application.DTOs.BancoEconomico.Requests;
using Application.DTOs.BancoEconomico.Responses;
using Application.Interfaces.Internal;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/qrsimple/notifyPaymentQR")]
public sealed class NotifyPaymentQrController(
    IBancoEconomicoService bancoEconomicoService,
    ILogger<NotifyPaymentQrController> logger
) : ControllerBase
{

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
            var response = await bancoEconomicoService.HandlePaymentNotificationAsync(
                request,
                cancellationToken
            );

            return Ok(response);
        }
        catch (ValidationException ex)
        {
            LogInvalidPayload(ex, request);
            return OkError(1, ex.Errors.FirstOrDefault()?.ErrorMessage ?? "Solicitud inválida.");
        }
        catch (ArgumentException ex)
        {
            LogInvalidPayload(ex, request);
            return OkError(1, ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error procesando la notificación QR de Banco Económico. Payload: {Payload}",
                SerializePayload(request));
            return OkError(99, "Ocurrió un error procesando la notificación.");
        }
    }

    private OkObjectResult OkError(int responseCode, string message) =>
        Ok(new NotifyPaymentQrResponseDto
        {
            ResponseCode = responseCode,
            Message = message
        });

    private void LogInvalidPayload(Exception ex, NotifyPaymentQrRequestDto request) =>
        logger.LogWarning(
            ex,
            "Banco Económico envió una notificación QR inválida. Payload: {Payload}",
            SerializePayload(request));

    /// <summary>
    /// Serializa el payload recibido a JSON compacto (una línea) para diagnóstico.
    /// Solo se invoca en el camino de error, por lo que no agrega volumen al log
    /// en notificaciones exitosas. Nunca lanza: ante cualquier fallo devuelve un
    /// marcador para no romper la respuesta hacia el banco.
    /// </summary>
    private static string SerializePayload(NotifyPaymentQrRequestDto? request)
    {
        try
        {
            return JsonSerializer.Serialize(request, NotifyPayloadJson.Options);
        }
        catch
        {
            return "<payload no serializable>";
        }
    }

    /// <summary>
    /// Opciones de serialización del payload de diagnóstico.
    /// </summary>
    private static class NotifyPayloadJson
    {
        public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
        {
            WriteIndented = false
        };
    }
}
