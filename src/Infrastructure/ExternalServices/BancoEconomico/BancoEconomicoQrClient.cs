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
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

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
        _logger.LogInformation("Iniciando autenticación contra Banco Económico.");

        var payload = new AuthenticateRequestDto
        {
            UserName = _options.UserName,
            Password = _options.EncryptedPassword
        };

        var result = await SendBanEcoAsync<AuthenticateResponseDto>(
            HttpMethod.Post,
            "api/authentication/authenticate",
            bearerToken: null,
            content: JsonContent.Create(payload),
            operation: "autenticación",
            httpError: "Error autenticando contra Banco Económico",
            nullError: "No se pudo deserializar la respuesta de autenticación de Banco Económico.",
            rejectedError: "Banco Económico rechazó la autenticación",
            functionalError: "Banco Económico devolvió error funcional en autenticación",
            cancellationToken
        );

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

        // La cuenta siempre la define el servidor; se envía una copia para no mutar el DTO recibido.
        var payload = new GenerateQrBankRequestDto
        {
            TransactionId = request.TransactionId,
            AccountCredit = _options.AccountCredit,
            Currency = request.Currency,
            Amount = request.Amount,
            Description = request.Description,
            DueDate = request.DueDate,
            SingleUse = request.SingleUse,
            ModifyAmount = request.ModifyAmount,
            BranchCode = request.BranchCode
        };

        var result = await SendBanEcoAsync<GenerateQrResponseDto>(
            HttpMethod.Post,
            "api/qrsimple/generateQR",
            bearerToken,
            JsonContent.Create(payload),
            operation: "generación de QR",
            httpError: "Error generando QR en Banco Económico",
            nullError: "No se pudo deserializar la respuesta de generación de QR de Banco Económico.",
            rejectedError: "Banco Económico rechazó la generación del QR",
            functionalError: "Banco Económico devolvió error funcional al generar QR",
            cancellationToken
        );

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

        var result = await SendBanEcoAsync<AnnulQrResponseDto>(
            HttpMethod.Delete,
            "api/qrsimple/cancelQR",
            bearerToken,
            JsonContent.Create(request),
            operation: "anulación de QR",
            httpError: "Error anulando QR en Banco Económico",
            nullError: "No se pudo deserializar la respuesta de anulación de QR de Banco Económico.",
            rejectedError: "Banco Económico rechazó la anulación del QR",
            functionalError: "Banco Económico devolvió error funcional al anular QR",
            cancellationToken
        );

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

        var result = await SendBanEcoAsync<QrStatusResponseDto>(
            HttpMethod.Get,
            $"api/qrsimple/v2/statusQR/{Uri.EscapeDataString(qrId)}",
            bearerToken,
            content: null,
            operation: "consulta de estado de QR",
            httpError: "Error consultando estado de QR en Banco Económico",
            nullError: "No se pudo deserializar la respuesta de estado de QR de Banco Económico.",
            rejectedError: "Banco Económico rechazó la consulta de estado del QR",
            functionalError: "Banco Económico devolvió error funcional al consultar estado de QR",
            cancellationToken
        );

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

        var result = await SendBanEcoAsync<PaidQrListResponseDto>(
            HttpMethod.Get,
            $"api/qrsimple/v2/paidQR/{Uri.EscapeDataString(fecha)}",
            bearerToken,
            content: null,
            operation: "consulta de QR pagados",
            httpError: "Error consultando QR pagados en Banco Económico",
            nullError: "No se pudo deserializar la respuesta de QR pagados de Banco Económico.",
            rejectedError: "Banco Económico rechazó la consulta de QR pagados",
            functionalError: "Banco Económico devolvió error funcional al consultar QR pagados",
            cancellationToken
        );

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

        var result = await SendBanEcoAsync<QueryMovementsResponseDto>(
            HttpMethod.Post,
            "api/accounts/queryMovements",
            bearerToken,
            JsonContent.Create(request),
            operation: "consulta de movimientos",
            httpError: "Error consultando movimientos en Banco Económico",
            nullError: "No se pudo deserializar la respuesta de movimientos de Banco Económico.",
            rejectedError: "Banco Económico rechazó la consulta de movimientos",
            functionalError: "Banco Económico devolvió error funcional al consultar movimientos",
            cancellationToken
        );

        result.AccountDetailList ??= [];
        result.AccountWithheldList ??= [];

        return result;
    }

    private async Task<TResponse> SendBanEcoAsync<TResponse>(
        HttpMethod method,
        string requestUri,
        string? bearerToken,
        HttpContent? content,
        string operation,
        string httpError,
        string nullError,
        string rejectedError,
        string functionalError,
        CancellationToken cancellationToken
    )
        where TResponse : IBanEcoResponse
    {
        using var httpRequest = new HttpRequestMessage(method, requestUri) { Content = content };

        if (!string.IsNullOrWhiteSpace(bearerToken))
        {
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Error HTTP en {Operation} de Banco Económico. StatusCode: {StatusCode}, Body: {Body}",
                operation,
                response.StatusCode,
                Truncate(responseContent, 500)
            );

            throw new HttpRequestException(
                $"{httpError}. StatusCode: {(int)response.StatusCode}. Body: {Truncate(responseContent, 500)}"
            );
        }

        var result = JsonSerializer.Deserialize<TResponse>(responseContent, JsonOptions);

        if (result is null)
        {
            throw new InvalidOperationException(nullError);
        }

        if (result.ResponseCode != 0)
        {
            _logger.LogWarning(
                "{FunctionalError}. ResponseCode: {ResponseCode}, Message: {Message}",
                functionalError,
                result.ResponseCode,
                result.Message
            );

            throw new InvalidOperationException(
                $"{rejectedError}. Código: {result.ResponseCode}, Mensaje: {result.Message}"
            );
        }

        return result;
    }

    private static string Truncate(string value, int maxLength)
    {
        if (value.Length <= maxLength)
        {
            return value;
        }

        return $"{value[..maxLength]}... (truncado)";
    }
}
