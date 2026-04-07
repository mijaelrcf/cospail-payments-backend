using System.Globalization;
using System.Text;
using System.Xml.Linq;
using Application.DTOs.Cospail;
using Application.Interfaces.External;
using CospailPaymentApi.Application.DTOs.Cospail;
using Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.ExternalServices.Cospail;

/// <summary>
/// Cliente SOAP manual para consumir el servicio de Cospail.
/// </summary>
public class CospailSoapClient : ICospailSoapClient
{
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
        var soapEnvelope = BuildGetDebtByFixedCodeEnvelope(fixedCode);

        using var request = new HttpRequestMessage(HttpMethod.Post, _options.BaseUrl);
        request.Headers.Add("SOAPAction", "\"http://sermix.net/ObtenerDeudaSocioCF\"");
        request.Content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");

        _logger.LogInformation(
            "Consultando deuda SOAP de Cospail para FixedCode: {FixedCode}",
            fixedCode
        );

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Error al consultar SOAP Cospail. StatusCode: {StatusCode}. Respuesta: {Response}",
                response.StatusCode,
                responseContent
            );

            throw new Exception("No se pudo consultar la deuda en Cospail.");
        }

        return ParseDebtResponse(responseContent, fixedCode);
    }

    private static string BuildGetDebtByFixedCodeEnvelope(int fixedCode)
    {
        return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
               xmlns:xsd=""http://www.w3.org/2001/XMLSchema""
               xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
  <soap:Body>
    <ObtenerDeudaSocioCF xmlns=""http://sermix.net/"">
      <liCfijo>{fixedCode}</liCfijo>
    </ObtenerDeudaSocioCF>
  </soap:Body>
</soap:Envelope>";
    }

    private static CospailDebtResponseDto ParseDebtResponse(string xml, int fixedCode)
    {
        var document = XDocument.Parse(xml);

        // Busca el primer nodo <Table> ignorando namespaces
        var tableElement = document.Descendants().FirstOrDefault(x => x.Name.LocalName == "Table");

        if (tableElement is null)
        {
            throw new KeyNotFoundException(
                "No se encontró información de deuda para el código fijo proporcionado."
            );
        }

        return new CospailDebtResponseDto
        {
            FixedCode = fixedCode,
            NoticeNumber = ParseNullableInt(GetElementValue(tableElement, "NAviso")),
            CreditNumber = ParseNullableInt(GetElementValue(tableElement, "NCredito")),
            Type = ParseNullableInt(GetElementValue(tableElement, "Tipo")),
            Year = ParseNullableInt(GetElementValue(tableElement, "Anio")),
            Month = ParseNullableInt(GetElementValue(tableElement, "Mes")),
            CustomerName = GetElementValue(tableElement, "Nombre") ?? string.Empty,
            Period = GetElementValue(tableElement, "Periodo") ?? string.Empty,
            Amount = ParseNullableDecimal(GetElementValue(tableElement, "Deuda"))
        };
    }

    private static string? GetElementValue(XElement parent, string elementName)
    {
        return parent.Elements().FirstOrDefault(x => x.Name.LocalName == elementName)?.Value;
    }

    private static int? ParseNullableInt(string? value)
    {
        if (int.TryParse(value, out var result))
        {
            return result;
        }

        return null;
    }

    private static decimal? ParseNullableDecimal(string? value)
    {
        if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        return null;
    }

    // ObtenerDeudaSocioDide: Consulta de deuda por codigo fijo y documento de identidad (CI/NIT)
    public async Task<GetMemberDebtByDocumentResponse> GetMemberDebtByDocumentAsync(
        int fixedCode,
        string documentId,
        CancellationToken cancellationToken = default
    )
    {
        var soapEnvelope = GetMemberDebtByDocumentEnvelope(fixedCode, documentId);

        using var request = new HttpRequestMessage(HttpMethod.Post, _options.BaseUrl);
        request.Headers.Add("SOAPAction", "\"http://sermix.net/ObtenerDeudaSocioDide\"");
        request.Content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");

        _logger.LogInformation(
            "Consultando deuda SOAP de Cospail para FixedCode: {FixedCode}",
            fixedCode
        );

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Error al consultar SOAP Cospail. StatusCode: {StatusCode}. Respuesta: {Response}",
                response.StatusCode,
                responseContent
            );

            throw new Exception("No se pudo consultar la deuda en Cospail.");
        }

        return ParseDebtByDocumentResponse(fixedCode, documentId, responseContent);
    }

    private static string GetMemberDebtByDocumentEnvelope(int fixedCode, string documentId)
    {
        return $"""
<?xml version="1.0" encoding="utf-8"?>
<soap:Envelope xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
               xmlns:xsd="http://www.w3.org/2001/XMLSchema"
               xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
  <soap:Body>
    <ObtenerDeudaSocioDide xmlns="http://sermix.net/">
      <liCFijo>{fixedCode}</liCFijo>
      <lsDide>{System.Security.SecurityElement.Escape(documentId)}</lsDide>
    </ObtenerDeudaSocioDide>
  </soap:Body>
</soap:Envelope>
""";
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
            DocumentId = documentId,
        };

        var soapDoc = XDocument.Parse(xml);

        XNamespace soapNs = "http://schemas.xmlsoap.org/soap/envelope/";
        XNamespace serviceNs = "http://sermix.net/";

        var resultElement = soapDoc
            .Descendants(soapNs + "Body")
            .Descendants(serviceNs + "ObtenerDeudaSocioDideResponse")
            .Descendants(serviceNs + "ObtenerDeudaSocioDideResult")
            .FirstOrDefault();

        if (resultElement is null)
        {
            throw new InvalidOperationException(
                "No se encontró ObtenerDeudaSocioDideResult en la respuesta SOAP."
            );
        }

        // Buscar todos los nodos Table del DataSet
        var tables = resultElement.Descendants().Where(x => x.Name.LocalName == "Table").ToList();

        if (!tables.Any())
        {
            result.Status = MemberDebtStatus.NoDebt;
            return result;
        }

        foreach (var table in tables)
        {
            var item = new DebtItemDto
            {
                NoticeNumber = ParseInt(table, "NAviso"),
                CreditNumber = ParseInt(table, "NCredito"),
                Type = ParseInt(table, "Tipo"),
                Year = ParseInt(table, "Anio"),
                Month = ParseInt(table, "Mes"),
                MemberName = ParseString(table, "Nombre"),
                Period = ParseString(table, "Periodo"),
                Amount = ParseDecimal(table, "Deuda")
            };

            result.Debts.Add(item);
        }

        var first = result.Debts.First();

        result.MemberName = string.IsNullOrWhiteSpace(first.MemberName) ? null : first.MemberName;

        // Interpretación de reglas de negocio de Cospail
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

    private static int ParseInt(XElement parent, string name)
    {
        var value = parent.Elements().FirstOrDefault(x => x.Name.LocalName == name)?.Value;
        return int.TryParse(value, out var number) ? number : 0;
    }

    private static decimal ParseDecimal(XElement parent, string name)
    {
        var value = parent.Elements().FirstOrDefault(x => x.Name.LocalName == name)?.Value;
        return decimal.TryParse(
            value,
            NumberStyles.Any,
            CultureInfo.InvariantCulture,
            out var number
        )
            ? number
            : 0m;
    }

    private static string ParseString(XElement parent, string name)
    {
        return parent.Elements().FirstOrDefault(x => x.Name.LocalName == name)?.Value
            ?? string.Empty;
    }
}
