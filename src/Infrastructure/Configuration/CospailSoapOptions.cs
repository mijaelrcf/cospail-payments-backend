namespace Infrastructure.Configuration;

/// <summary>
/// Configuración del servicio SOAP de Cospail.
/// </summary>
public class CospailSoapOptions
{
    public const string SectionName = "ExternalServices:CospailSoap";

    public string BaseUrl { get; set; } = string.Empty;
}