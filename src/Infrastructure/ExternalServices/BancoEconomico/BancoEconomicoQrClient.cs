using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Application.DTOs.BancoEconomico.Requests;
using Application.DTOs.BancoEconomico.Responses;
using Application.Interfaces.External;
using Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.ExternalServices.BancoEconomico;

/// <summary>
/// Cliente HTTP para integración con Banco Económico.
/// </summary>
public sealed class BancoEconomicoQrClient : IBancoEconomicoQrClient
{
    private readonly HttpClient _httpClient;
    private readonly BancoEconomicoOptions _options;
    private readonly ILogger<BancoEconomicoQrClient> _logger;

    public BancoEconomicoQrClient(
        HttpClient httpClient,
        IOptions<BancoEconomicoOptions> options,
        ILogger<BancoEconomicoQrClient> logger
    )
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Autentica contra Banco Económico y obtiene un token Bearer.
    /// </summary>
    public async Task<AuthenticateResponseDto> AuthenticateAsync(
        CancellationToken cancellationToken = default
    )
    {
        var request = new AuthenticateRequestDto
        {
            UserName = _options.UserName,
            Password = _options.EncryptedPassword
        };

        _logger.LogInformation("Iniciando autenticación contra Banco Económico.");

        using var response = await _httpClient.PostAsJsonAsync(
            "api/authentication/authenticate",
            request,
            cancellationToken
        );

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Error HTTP autenticando contra Banco Económico. StatusCode: {StatusCode}, Body: {Body}",
                response.StatusCode,
                responseContent
            );

            throw new HttpRequestException(
                $"Error autenticando contra Banco Económico. StatusCode: {(int)response.StatusCode}. Body: {responseContent}"
            );
        }

        var result = JsonSerializer.Deserialize<AuthenticateResponseDto>(
            responseContent,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        if (result is null)
        {
            throw new InvalidOperationException(
                "No se pudo deserializar la respuesta de autenticación de Banco Económico."
            );
        }

        if (result.ResponseCode != 0)
        {
            _logger.LogWarning(
                "Banco Económico devolvió error funcional en autenticación. ResponseCode: {ResponseCode}, Message: {Message}",
                result.ResponseCode,
                result.Message
            );

            throw new InvalidOperationException(
                $"Banco Económico rechazó la autenticación. Código: {result.ResponseCode}, Mensaje: {result.Message}"
            );
        }

        _logger.LogInformation("Autenticación con Banco Económico exitosa.");

        return result;
    }

    /// <summary>
    /// Genera un código QR en Banco Económico.
    /// </summary>
    public async Task<GenerateQrResponseDto> GenerateQrAsync(
        string bearerToken,
        GenerateQrBankRequestDto request,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogInformation(
            "Iniciando generación de QR en Banco Económico. TransactionId: {TransactionId}, Amount: {Amount}, Currency: {Currency}",
            request.TransactionId,
            request.Amount,
            request.Currency
        );

        request.AccountCredit = _options.AccountCredit;

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "api/qrsimple/generateQR")
        {
            Content = JsonContent.Create(request)
        };

        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Error HTTP generando QR en Banco Económico. StatusCode: {StatusCode}, Body: {Body}",
                response.StatusCode,
                responseContent
            );

            throw new HttpRequestException(
                $"Error generando QR en Banco Económico. StatusCode: {(int)response.StatusCode}. Body: {responseContent}"
            );
        }

        var result = JsonSerializer.Deserialize<GenerateQrResponseDto>(
            responseContent,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        if (result is null)
        {
            throw new InvalidOperationException(
                "No se pudo deserializar la respuesta de generación de QR de Banco Económico."
            );
        }

        if (result.ResponseCode != 0)
        {
            _logger.LogWarning(
                "Banco Económico devolvió error funcional al generar QR. ResponseCode: {ResponseCode}, Message: {Message}",
                result.ResponseCode,
                result.Message
            );

            throw new InvalidOperationException(
                $"Banco Económico rechazó la generación del QR. Código: {result.ResponseCode}, Mensaje: {result.Message}"
            );
        }

        _logger.LogInformation(
            "QR generado exitosamente en Banco Económico. QrId: {QrId}",
            result.QrId
        );

        return result;
    }

    /// <summary>
    /// Anula un código QR pendiente en Banco Económico.
    /// </summary>
    public async Task<AnnulQrResponseDto> AnnulQrAsync(
        string bearerToken,
        AnnulQrBankRequestDto request,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogInformation(
            "Iniciando anulación de QR en Banco Económico. QrId: {QrId}",
            request.QrId
        );

        using var httpRequest = new HttpRequestMessage(HttpMethod.Delete, "api/qrsimple/cancelQR")
        {
            Content = JsonContent.Create(request)
        };

        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Error HTTP anulando QR en Banco Económico. StatusCode: {StatusCode}, Body: {Body}",
                response.StatusCode,
                responseContent
            );

            throw new HttpRequestException(
                $"Error anulando QR en Banco Económico. StatusCode: {(int)response.StatusCode}. Body: {responseContent}"
            );
        }

        var result = JsonSerializer.Deserialize<AnnulQrResponseDto>(
            responseContent,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        if (result is null)
        {
            throw new InvalidOperationException(
                "No se pudo deserializar la respuesta de anulación de QR de Banco Económico."
            );
        }

        if (result.ResponseCode != 0)
        {
            _logger.LogWarning(
                "Banco Económico devolvió error funcional al anular QR. ResponseCode: {ResponseCode}, Message: {Message}",
                result.ResponseCode,
                result.Message
            );

            throw new InvalidOperationException(
                $"Banco Económico rechazó la anulación del QR. Código: {result.ResponseCode}, Mensaje: {result.Message}"
            );
        }

        _logger.LogInformation(
            "QR anulado exitosamente en Banco Económico. QrId: {QrId}",
            request.QrId
        );

        return result;
    }

    /// <summary>
    /// Consulta el estado actual de un código QR (7.4 statusQR).
    /// </summary>
    public async Task<QrStatusResponseDto> GetQrStatusAsync(
        string bearerToken,
        string qrId,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogInformation(
            "Consultando estado de QR en Banco Económico. QrId: {QrId}",
            qrId
        );

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/qrsimple/v2/statusQR/{Uri.EscapeDataString(qrId)}"
        );

        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Error HTTP consultando estado de QR en Banco Económico. StatusCode: {StatusCode}, Body: {Body}",
                response.StatusCode,
                responseContent
            );

            throw new HttpRequestException(
                $"Error consultando estado de QR en Banco Económico. StatusCode: {(int)response.StatusCode}. Body: {responseContent}"
            );
        }

        var result = JsonSerializer.Deserialize<QrStatusResponseDto>(
            responseContent,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        if (result is null)
        {
            throw new InvalidOperationException(
                "No se pudo deserializar la respuesta de estado de QR de Banco Económico."
            );
        }

        if (result.ResponseCode != 0)
        {
            _logger.LogWarning(
                "Banco Económico devolvió error funcional al consultar estado de QR. ResponseCode: {ResponseCode}, Message: {Message}",
                result.ResponseCode,
                result.Message
            );

            throw new InvalidOperationException(
                $"Banco Económico rechazó la consulta de estado del QR. Código: {result.ResponseCode}, Mensaje: {result.Message}"
            );
        }

        result.Payment ??= [];

        return result;
    }

    /// <summary>
    /// Retorna el listado de QR pagados en una fecha (7.6 paidQR).
    /// </summary>
    public async Task<PaidQrListResponseDto> GetPaidQrListAsync(
        string bearerToken,
        string fecha,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogInformation(
            "Consultando QR pagados en Banco Económico. Fecha: {Fecha}",
            fecha
        );

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/qrsimple/v2/paidQR/{fecha}"
        );

        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Error HTTP consultando QR pagados en Banco Económico. StatusCode: {StatusCode}, Body: {Body}",
                response.StatusCode,
                responseContent
            );

            throw new HttpRequestException(
                $"Error consultando QR pagados en Banco Económico. StatusCode: {(int)response.StatusCode}. Body: {responseContent}"
            );
        }

        var result = JsonSerializer.Deserialize<PaidQrListResponseDto>(
            responseContent,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        if (result is null)
        {
            throw new InvalidOperationException(
                "No se pudo deserializar la respuesta de QR pagados de Banco Económico."
            );
        }

        if (result.ResponseCode != 0)
        {
            _logger.LogWarning(
                "Banco Económico devolvió error funcional al consultar QR pagados. ResponseCode: {ResponseCode}, Message: {Message}",
                result.ResponseCode,
                result.Message
            );

            throw new InvalidOperationException(
                $"Banco Económico rechazó la consulta de QR pagados. Código: {result.ResponseCode}, Mensaje: {result.Message}"
            );
        }

        result.PaymentList ??= [];

        return result;
    }

    /// <summary>
    /// Consulta los movimientos de una cuenta por período (8.1 queryMovements).
    /// </summary>
    public async Task<QueryMovementsResponseDto> QueryMovementsAsync(
        string bearerToken,
        QueryMovementsBankRequestDto request,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogInformation(
            "Consultando movimientos en Banco Económico. StartDate: {StartDate}, EndDate: {EndDate}",
            request.StartDate,
            request.EndDate
        );

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "api/accounts/queryMovements")
        {
            Content = JsonContent.Create(request)
        };

        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Error HTTP consultando movimientos en Banco Económico. StatusCode: {StatusCode}, Body: {Body}",
                response.StatusCode,
                responseContent
            );

            throw new HttpRequestException(
                $"Error consultando movimientos en Banco Económico. StatusCode: {(int)response.StatusCode}. Body: {responseContent}"
            );
        }

        var result = JsonSerializer.Deserialize<QueryMovementsResponseDto>(
            responseContent,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        if (result is null)
        {
            throw new InvalidOperationException(
                "No se pudo deserializar la respuesta de movimientos de Banco Económico."
            );
        }

        if (result.ResponseCode != 0)
        {
            _logger.LogWarning(
                "Banco Económico devolvió error funcional al consultar movimientos. ResponseCode: {ResponseCode}, Message: {Message}",
                result.ResponseCode,
                result.Message
            );

            throw new InvalidOperationException(
                $"Banco Económico rechazó la consulta de movimientos. Código: {result.ResponseCode}, Mensaje: {result.Message}"
            );
        }

        result.AccountDetailList ??= [];
        result.AccountWithheldList ??= [];

        return result;
    }
}
