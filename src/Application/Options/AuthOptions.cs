namespace Application.Options;

/// <summary>
/// Configuración de autenticación JWT para el panel de administración.
/// </summary>
public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public string Issuer { get; set; } = "cospail-admin";
    public string Audience { get; set; } = "cospail-payments-api";

    /// <summary>
    /// Clave secreta de firma HMAC-SHA256. Debe tener al menos 32 bytes (256 bits).
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Vigencia del token emitido, en minutos.
    /// </summary>
    public int TokenLifetimeMinutes { get; set; } = 120;

    /// <summary>
    /// Usuarios autorizados para el panel de administración.
    /// </summary>
    public List<AuthUserOptions> Users { get; set; } = [];
}
