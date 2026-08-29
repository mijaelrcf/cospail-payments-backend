namespace Application.Options;

/// <summary>
/// Usuario del panel de administración definido en configuración.
/// </summary>
public sealed class AuthUserOptions
{
    /// <summary>
    /// Nombre de usuario para iniciar sesión.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Hash PBKDF2 del password con el formato
    /// <c>PBKDF2$iteraciones$saltBase64$hashBase64</c>. Nunca se guarda el password en claro.
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Nombre legible que se mostrará en el panel.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
}
