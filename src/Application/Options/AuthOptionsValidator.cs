using System.Text;
using Microsoft.Extensions.Options;

namespace Application.Options;

/// <summary>
/// Reglas de <see cref="AuthOptions"/> que no se pueden expresar con atributos:
/// longitud en bytes de la clave y unicidad de usuarios.
/// </summary>
public sealed class AuthOptionsValidator : IValidateOptions<AuthOptions>
{
    public ValidateOptionsResult Validate(string? name, AuthOptions options)
    {
        var secretKeyBytes = Encoding.UTF8.GetBytes(options.SecretKey ?? string.Empty);
        if (secretKeyBytes.Length < 32)
        {
            return ValidateOptionsResult.Fail(
                "La configuración 'Auth:SecretKey' es obligatoria y debe tener al menos 256 bits. " +
                $"El valor efectivamente cargado solo tiene {secretKeyBytes.Length * 8} bits. " +
                "Definela en los secrets de desarrollo o en una variable de entorno (Auth__SecretKey); " +
                "si definiste varias, recuerda que tienen precedencia las variables de entorno y luego los secrets."
            );
        }

        var duplicates = options
            .Users.Where(u => !string.IsNullOrWhiteSpace(u.Username))
            .GroupBy(u => u.Username, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicates.Count > 0)
        {
            return ValidateOptionsResult.Fail(
                $"La configuración 'Auth:Users' tiene nombres de usuario duplicados: {string.Join(", ", duplicates)}."
            );
        }

        return ValidateOptionsResult.Success;
    }
}
