using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Configuration;

/// <summary>
/// Opciones de configuración para la integración con Banco Económico.
/// </summary>
public sealed class BancoEconomicoOptions
{
    public const string SectionName = "ExternalServices:BanEcoApi";

    /// <summary>
    /// URL base del API Gateway del banco.
    /// Ejemplo: https://apimktdesa.baneco.com.bo/ApiGateway/
    /// </summary>
    [Required, Url]
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Usuario entregado por el banco.
    /// </summary>
    [Required]
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Password cifrado que se enviará al authenticate.
    /// </summary>
    [Required]
    public string EncryptedPassword { get; set; } = string.Empty;

    /// <summary>
    /// Cuenta que se enviará para generar el codigo QR.
    /// </summary>
    [Required]
    public string AccountCredit { get; set; } = string.Empty;

    /// <summary>
    /// Horas de vigencia del QR generado a partir de ahora.
    /// 0 = vence hoy (hora Bolivia), 24 = vence mañana, 48 = en dos días, etc.
    /// </summary>
    [Range(0, 168)]
    public int QrValidityHours { get; set; }
}