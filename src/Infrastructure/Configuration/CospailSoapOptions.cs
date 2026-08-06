namespace Infrastructure.Configuration;

/// <summary>
/// Configuración del servicio SOAP de Cospail.
/// </summary>
public sealed class CospailSoapOptions
{
    public const string SectionName = "ExternalServices:CospailSoap";

    public string BaseUrl { get; set; } = string.Empty;
    public string Login { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}