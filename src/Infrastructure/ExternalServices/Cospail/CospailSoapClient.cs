using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Application.DTOs.Cospail.Common;
using Application.DTOs.Cospail.Requests;
using Application.DTOs.Cospail.Responses;
using Application.Interfaces.External;
using Domain.Entities;
using Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.ExternalServices.Cospail;

/// <summary>
/// Cliente SOAP manual para consumir el servicio de Cospail.
/// </summary>
public sealed class CospailSoapClient : ICospailSoapClient
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
    /// Obtiene el reporte de cobros de un socio en un rango de fechas
    /// mediante ObtenerCobrosFecha.
    /// </summary>
    public async Task<List<InvoiceSummaryDto>> GetChargesByDateAsync(
        int fixedCode,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default
    )
    {
        const string operationName = "ObtenerCobrosFecha";

        var parameters = new Dictionary<string, string>
        {
            ["liCfijo"] = fixedCode.ToString(CultureInfo.InvariantCulture),
            ["FechaDesde"] = from.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
            ["FechaHasta"] = to.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
            ["lsLogin"] = _options.Login,
            ["lsPassword"] = _options.Password
        };

        var xml = await SendSoapRequestAsync(operationName, parameters, cancellationToken);

        return ParseChargesByDateResponse(xml);
    }

    /// <summary>
    /// Obtiene el PDF (Base64) de una factura mediante obtenerUnaFacturaPDFB64.
    /// El parámetro SOAP es NCredito y su valor es el IDCredito del reporte
    /// (son lo mismo).
    /// </summary>
    public async Task<InvoicePdfDto> GetInvoicePdfBase64Async(
        int creditNumber,
        CancellationToken cancellationToken = default
    )
    {
        const string operationName = "obtenerUnaFacturaPDFB64";

        var parameters = new Dictionary<string, string>
        {
            ["NCredito"] = creditNumber.ToString(CultureInfo.InvariantCulture)
        };

        var xml = await SendSoapRequestAsync(operationName, parameters, cancellationToken);

        return ParseInvoicePdfResponse(creditNumber, xml);
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
            ["lsLogin"] = _options.Login,
            ["lsPassword"] = _options.Password
        };

        var xml = await SendSoapRequestAsync(operationName, parameters, cancellationToken);

        return ParseRecordPaymentResponse(xml);
    }

    private async Task<string> SendSoapRequestAsync(
        string operationName,
        IReadOnlyDictionary<string, string> parameters,
        CancellationToken cancellationToken
    )
    {
        var soapEnvelope = BuildSoapEnvelope(operationName, parameters);

        using var request = new HttpRequestMessage(HttpMethod.Post, string.Empty);
        request.Headers.Add("SOAPAction", $"\"{ServiceNamespace}{operationName}\"");
        request.Content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");

        _logger.LogInformation(
            "Consumiendo SOAP Cospail. Operación: {OperationName}",
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
                Truncate(responseContent, 500)
            );

            throw new InvalidOperationException($"No se pudo consumir la operación SOAP {operationName}.");
        }

        return responseContent;
    }

    private static string Truncate(string value, int maxLength)
    {
        if (value.Length <= maxLength)
        {
            return value;
        }

        return $"{value[..maxLength]}... (truncado)";
    }

    private static string BuildSoapEnvelope(
        string operationName,
        IReadOnlyDictionary<string, string> parameters
    )
    {
        var builder = new StringBuilder(capacity: 512 + parameters.Count * 64);
        builder.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        builder.AppendLine("<soap:Envelope xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"");
        builder.AppendLine("               xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"");
        builder.AppendLine($"               xmlns:soap=\"{SoapEnvelopeNamespace}\">");
        builder.AppendLine("  <soap:Body>");
        builder.AppendLine($"""    <{operationName} xmlns="{ServiceNamespace}">""");

        foreach (var (key, value) in parameters)
        {
            builder.Append("      <").Append(key).Append('>');
            builder.Append(System.Security.SecurityElement.Escape(value));
            builder.Append("</").Append(key).AppendLine(">");
        }

        builder.Append("    </").Append(operationName).AppendLine(">");
        builder.AppendLine("  </soap:Body>");
        builder.AppendLine("</soap:Envelope>");

        return builder.ToString();
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

    private static List<InvoiceSummaryDto> ParseChargesByDateResponse(string xml)
    {
        var resultElement = GetSoapResultElement(xml, "ObtenerCobrosFecha");
        var tables = GetTableElements(resultElement).ToList();

        var invoices = new List<InvoiceSummaryDto>(tables.Count);

        foreach (var table in tables)
        {
            // Campos reales SOAP UI: codCobrador, IDCredito, FechaPago, HoraPago, CodigoFijo, Nombre, Importe.
            var creditNumber = ParseInt(table, "IDCredito");
            var amount = ParseDecimal(table, "Importe");
            var memberName = ParseString(table, "Nombre");
            var collectorCode = ParseInt(table, "codCobrador");
            var fixedCode = ParseInt(table, "CodigoFijo");
            var paymentDate = ParseString(table, "FechaPago");
            var paymentTime = ParseString(table, "HoraPago");
            var chargeDate = CombinePaymentDateTime(paymentDate, paymentTime);

            // Las filas vacías (sin IDCredito e importe cero) no son facturas.
            if (creditNumber <= 0 && amount == 0)
            {
                continue;
            }

            invoices.Add(new InvoiceSummaryDto
            {
                CreditNumber = creditNumber,
                ChargeDate = chargeDate,
                PaymentTime = paymentTime,
                Amount = amount,
                MemberName = memberName,
                CollectorCode = collectorCode,
                FixedCode = fixedCode
            });
        }

        return invoices
            .OrderByDescending(x => x.ChargeDate ?? DateTime.MinValue)
            .ThenByDescending(x => x.CreditNumber)
            .ToList();
    }

    private static DateTime? CombinePaymentDateTime(string paymentDate, string paymentTime)
    {
        if (string.IsNullOrWhiteSpace(paymentDate))
        {
            return null;
        }

        var normalizedTime = NormalizePaymentTime(paymentTime);
        var combined = string.IsNullOrWhiteSpace(normalizedTime)
            ? paymentDate.Trim()
            : $"{paymentDate.Trim()} {normalizedTime}";

        string[] formats =
        [
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-dd HH:mm",
            "yyyy-MM-dd",
            "dd/MM/yyyy HH:mm:ss",
            "dd/MM/yyyy HH:mm",
            "dd/MM/yyyy"
        ];

        return DateTime.TryParseExact(
            combined,
            formats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var date)
            ? date
            : null;
    }

    private static string NormalizePaymentTime(string paymentTime)
    {
        if (string.IsNullOrWhiteSpace(paymentTime))
        {
            return string.Empty;
        }

        var time = paymentTime.Trim();

        // SOAP UI devuelve a veces "16:15:" (con : final). Completar a HH:mm:ss.
        while (time.EndsWith(":", StringComparison.Ordinal))
        {
            time += "00";
        }

        // "HH:mm" -> "HH:mm:00" para parseo uniforme.
        if (time.Length == 5 && time[2] == ':')
        {
            time += ":00";
        }

        return time;
    }

    private static InvoicePdfDto ParseInvoicePdfResponse(int creditNumber, string xml)
    {
        var resultElement = GetSoapResultElement(xml, "obtenerUnaFacturaPDFB64");
        var rawResult = resultElement.Value?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(rawResult))
        {
            throw new KeyNotFoundException(
                $"No se encontró la factura para el crédito {creditNumber}."
            );
        }

        try
        {
            Convert.FromBase64String(rawResult);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException(
                "La respuesta de Cospail no contiene un PDF Base64 válido.", ex
            );
        }

        return new InvoicePdfDto
        {
            CreditNumber = creditNumber,
            FileName = $"factura-{creditNumber}.pdf",
            ContentType = "application/pdf",
            PdfBase64 = rawResult
        };
    }

    private static XElement GetSoapResultElement(string xml, string operationName)
    {
        using var reader = XmlReader.Create(
            new StringReader(xml),
            new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null }
        );
        var document = XDocument.Load(reader);

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

    private static string? GetElementValue(XElement parent, params string[] names)
    {
        var elements = parent.Elements().ToList();
        foreach (var name in names)
        {
            var match = elements.FirstOrDefault(x =>
                string.Equals(x.Name.LocalName, name, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
            {
                return match.Value;
            }
        }

        return null;
    }

    private static int ParseInt(XElement parent, params string[] names)
    {
        var value = GetElementValue(parent, names);
        return int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var number)
            ? number
            : 0;
    }

    private static decimal ParseDecimal(XElement parent, params string[] names)
    {
        var value = GetElementValue(parent, names);
        return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var number)
            ? number
            : 0m;
    }

    private static string ParseString(XElement parent, params string[] names)
    {
        return GetElementValue(parent, names) ?? string.Empty;
    }
}
