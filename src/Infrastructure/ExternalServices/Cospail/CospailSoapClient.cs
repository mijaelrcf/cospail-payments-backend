using System.Globalization;
using System.Text;
using System.Xml.Linq;
using Application.DTOs.Cospail.Common;
using Application.DTOs.Cospail.Requests;
using Application.DTOs.Cospail.Responses;
using Application.Interfaces.External;
using Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.ExternalServices.Cospail;

/// <summary>
/// Cliente SOAP manual para consumir el servicio de Cospail.
/// </summary>
public class CospailSoapClient : ICospailSoapClient
{
    private const string ServiceNamespace = "http://sermix.net/";
    private const string SoapEnvelopeNamespace = "http://schemas.xmlsoap.org/soap/envelope/";

    private readonly HttpClient _httpClient;
    private readonly CospailSoapOptions _options;
    private readonly ILogger<CospailSoapClient> _logger;

    public CospailSoapClient(
        HttpClient httpClient,
        IOptions<CospailSoapOptions> options,
        ILogger<CospailSoapClient> logger
    )
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<CospailDebtResponseDto> GetDebtByFixedCodeAsync(
        int fixedCode,
        CancellationToken cancellationToken = default
    )
    {
        var operationName = "ObtenerDeudaSocioCF";

        var parameters = new Dictionary<string, string> { ["liCfijo"] = fixedCode.ToString() };

        var xml = await SendSoapRequestAsync(operationName, parameters, cancellationToken);

        return ParseDebtByFixedCodeResponse(xml, fixedCode);
    }

    public async Task<GetMemberDebtByDocumentResponse> GetMemberDebtByDocumentAsync(
        int fixedCode,
        string documentId,
        CancellationToken cancellationToken = default
    )
    {
        var operationName = "ObtenerDeudaSocioDide";

        var parameters = new Dictionary<string, string>
        {
            ["liCFijo"] = fixedCode.ToString(),
            ["lsDide"] = documentId
        };

        var xml = await SendSoapRequestAsync(operationName, parameters, cancellationToken);

        return ParseDebtByDocumentResponse(fixedCode, documentId, xml);
    }

    /// <summary>
    /// Registra el cobro de una deuda en Cospail mediante grabarCobrosWEB.
    /// </summary>
    public async Task<RecordPaymentResponseDto> RecordPaymentAsync(
        RecordPaymentRequestDto requestDto,
        CancellationToken cancellationToken = default
    )
    {
        var operationName = "grabarCobrosWEB";

        var paymentDate = requestDto
            .PaymentDate
            .ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
        var paymentTime = requestDto.PaymentTime;

        var parameters = new Dictionary<string, string>
        {
            ["NCredito"] = requestDto.CreditNumber.ToString(),
            ["Tipo"] = requestDto.Type.ToString(),
            ["Deuda"] = requestDto.Amount.ToString("0.00", CultureInfo.InvariantCulture),
            ["ldFpag"] = paymentDate,
            ["lsHpag"] = paymentTime,
            ["lsLogin"] = _options.Login ?? string.Empty,
            ["lsPassword"] = _options.Password ?? string.Empty
        };

        var xml = await SendSoapRequestAsync(operationName, parameters, cancellationToken);

        return ParseRecordPaymentResponse(xml);
    }

    private async Task<string> SendSoapRequestAsync(
        string operationName,
        Dictionary<string, string> parameters,
        CancellationToken cancellationToken
    )
    {
        var soapEnvelope = BuildSoapEnvelope(operationName, parameters);

        using var request = new HttpRequestMessage(HttpMethod.Post, _options.BaseUrl);
        request.Headers.Add("SOAPAction", $"\"{ServiceNamespace}{operationName}\"");
        request.Content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");

        _logger.LogInformation(
            "Consumiento SOAP Cospail. Operación: {OperationName}",
            operationName
        );

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Error al consumir SOAP Cospail. Operación: {OperationName}. StatusCode: {StatusCode}. Respuesta: {Response}",
                operationName,
                response.StatusCode,
                responseContent
            );

            throw new InvalidOperationException($"No se pudo consumir la operación SOAP {operationName}.");
        }

        return responseContent;
    }

    private static string BuildSoapEnvelope(
        string operationName,
        Dictionary<string, string> parameters
    )
    {
        var parametersXml = string.Join(
            Environment.NewLine,
            parameters.Select(
                x => $"      <{x.Key}>{System.Security.SecurityElement.Escape(x.Value)}</{x.Key}>"
            )
        );

        return $"""
<?xml version="1.0" encoding="utf-8"?>
<soap:Envelope xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
               xmlns:xsd="http://www.w3.org/2001/XMLSchema"
               xmlns:soap="{SoapEnvelopeNamespace}">
  <soap:Body>
    <{operationName} xmlns="{ServiceNamespace}">
{parametersXml}
    </{operationName}>
  </soap:Body>
</soap:Envelope>
""";
    }

    private static CospailDebtResponseDto ParseDebtByFixedCodeResponse(string xml, int fixedCode)
    {
        var resultElement = GetSoapResultElement(xml, "ObtenerDeudaSocioCF");
        var tableElement = GetTableElements(resultElement).FirstOrDefault();

        if (tableElement is null)
        {
            throw new KeyNotFoundException(
                "No se encontró información de deuda para el código fijo proporcionado."
            );
        }

        return new CospailDebtResponseDto
        {
            FixedCode = fixedCode,
            NoticeNumber = ParseNullableInt(tableElement, "NAviso"),
            CreditNumber = ParseNullableInt(tableElement, "NCredito"),
            Type = ParseNullableInt(tableElement, "Tipo"),
            Year = ParseNullableInt(tableElement, "Anio"),
            Month = ParseNullableInt(tableElement, "Mes"),
            CustomerName = ParseString(tableElement, "Nombre"),
            Period = ParseString(tableElement, "Periodo"),
            Amount = ParseNullableDecimal(tableElement, "Deuda")
        };
    }

    private static GetMemberDebtByDocumentResponse ParseDebtByDocumentResponse(
        int fixedCode,
        string documentId,
        string xml
    )
    {
        var result = new GetMemberDebtByDocumentResponse
        {
            FixedCode = fixedCode,
            DocumentId = documentId
        };

        var resultElement = GetSoapResultElement(xml, "ObtenerDeudaSocioDide");
        var tables = GetTableElements(resultElement).ToList();

        if (!tables.Any())
        {
            result.Status = MemberDebtStatus.NoDebt;
            return result;
        }

        foreach (var table in tables)
        {
            result
                .Debts
                .Add(
                    new DebtItemDto
                    {
                        NoticeNumber = ParseInt(table, "NAviso"),
                        CreditNumber = ParseInt(table, "NCredito"),
                        Type = ParseInt(table, "Tipo"),
                        Year = ParseInt(table, "Anio"),
                        Month = ParseInt(table, "Mes"),
                        MemberName = ParseString(table, "Nombre"),
                        Period = ParseString(table, "Periodo"),
                        Amount = ParseDecimal(table, "Deuda")
                    }
                );
        }

        var first = result.Debts.First();

        result.MemberName = string.IsNullOrWhiteSpace(first.MemberName) ? null : first.MemberName;

        if (
            first.Amount == -1
            || (
                first.MemberName?.Contains("NO EXISTE", StringComparison.OrdinalIgnoreCase) ?? false
            )
        )
        {
            result.Status = MemberDebtStatus.MemberNotFound;
            result.Debts.Clear();
            result.MemberName = null;
            return result;
        }

        if (
            (
                first.Period?.Contains("NO COINCIDE CI/NIT", StringComparison.OrdinalIgnoreCase)
                ?? false
            )
            && first.Amount == 0
        )
        {
            result.Status = MemberDebtStatus.DocumentMismatch;
            result.Debts.Clear();
            return result;
        }

        if (
            (first.Period?.Contains("SIN DEUDA", StringComparison.OrdinalIgnoreCase) ?? false)
            && first.Amount == 0
        )
        {
            result.Status = MemberDebtStatus.NoDebt;
            result.Debts.Clear();
            return result;
        }

        result.Status = MemberDebtStatus.HasDebt;
        return result;
    }

    private static RecordPaymentResponseDto ParseRecordPaymentResponse(string xml)
    {
        var resultElement = GetSoapResultElement(xml, "grabarCobrosWEB");
        var rawResult = resultElement.Value?.Trim() ?? string.Empty;

        return new RecordPaymentResponseDto
        {
            RawResult = rawResult,
            Success = rawResult == "1",
            Message = rawResult == "1" ? "Cobro registrado correctamente." : rawResult
        };
    }

    private static XElement GetSoapResultElement(string xml, string operationName)
    {
        var document = XDocument.Parse(xml);

        XNamespace soapNs = SoapEnvelopeNamespace;
        XNamespace serviceNs = ServiceNamespace;

        var resultElement = document
            .Descendants(soapNs + "Body")
            .Descendants(serviceNs + $"{operationName}Response")
            .Descendants(serviceNs + $"{operationName}Result")
            .FirstOrDefault();

        if (resultElement is null)
        {
            throw new InvalidOperationException(
                $"No se encontró {operationName}Result en la respuesta SOAP."
            );
        }

        return resultElement;
    }

    private static IEnumerable<XElement> GetTableElements(XElement resultElement)
    {
        return resultElement.Descendants().Where(x => x.Name.LocalName == "Table");
    }

    private static string? GetElementValue(XElement parent, string elementName)
    {
        return parent.Elements().FirstOrDefault(x => x.Name.LocalName == elementName)?.Value;
    }

    private static int ParseInt(XElement parent, string name)
    {
        var value = GetElementValue(parent, name);
        return int.TryParse(value, out var number) ? number : 0;
    }

    private static int? ParseNullableInt(XElement parent, string name)
    {
        var value = GetElementValue(parent, name);
        return int.TryParse(value, out var number) ? number : null;
    }

    private static decimal ParseDecimal(XElement parent, string name)
    {
        var value = GetElementValue(parent, name);
        return decimal.TryParse(
            value,
            NumberStyles.Any,
            CultureInfo.InvariantCulture,
            out var number
        )
            ? number
            : 0m;
    }

    private static decimal? ParseNullableDecimal(XElement parent, string name)
    {
        var value = GetElementValue(parent, name);
        return decimal.TryParse(
            value,
            NumberStyles.Any,
            CultureInfo.InvariantCulture,
            out var number
        )
            ? number
            : null;
    }

    private static string ParseString(XElement parent, string name)
    {
        return GetElementValue(parent, name) ?? string.Empty;
    }
}
