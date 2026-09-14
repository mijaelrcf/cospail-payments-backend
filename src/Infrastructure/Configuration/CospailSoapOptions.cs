using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Configuration;

/// <summary>
/// Configuración del servicio SOAP de Cospail.
/// </summary>
public sealed class CospailSoapOptions
{
    public const string SectionName = "ExternalServices:CospailSoap";

    [Required, Url]
    public string BaseUrl { get; set; } = string.Empty;

    [Required]
    public string Login { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}