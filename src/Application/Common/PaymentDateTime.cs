using System.Globalization;

namespace Application.Common;

/// <summary>
/// Formatos de fecha/hora que Banco Económico envía en las notificaciones de pago.
/// Lógica compartida entre el servicio (lanza excepción) y el validador (retorna bool).
/// </summary>
public static class PaymentDateTime
{
    public const string DateFormat = "yyyy-MM-dd";
    public const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ss";
    public const string TimeFormat = "HH:mm:ss";
    public const string ShortTimeFormat = "HH:mm";
    public const string CompactDateFormat = "yyyyMMdd";

    /// <summary>
    /// Offset de Bolivia (UTC-04:00), sin horario de verano.
    /// </summary>
    public static readonly TimeSpan BoliviaUtcOffset = TimeSpan.FromHours(-4);

    /// <summary>
    /// Intenta extraer solo la fecha de <c>paymentDate</c>.
    /// Acepta <c>yyyy-MM-dd</c>, <c>yyyy-MM-ddTHH:mm:ss</c> e ISO 8601 con zona.
    /// </summary>
    public static bool TryParseDate(string? value, out DateOnly date)
    {
        var trimmed = (value ?? string.Empty).Trim();

        if (DateOnly.TryParseExact(trimmed, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
        {
            return true;
        }

        if (DateTime.TryParseExact(
            trimmed,
            DateTimeFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var dateTime))
        {
            date = DateOnly.FromDateTime(dateTime);
            return true;
        }

        // ISO 8601 con zona, ej. "2026-09-07T04:00:00Z" o "2026-09-07T00:00:00-04:00".
        // Se exige prefijo de año para no aceptar formatos como "14/07/2026".
        if (trimmed.StartsWith("20", StringComparison.Ordinal) || trimmed.StartsWith("19", StringComparison.Ordinal))
        {
            if (DateTimeOffset.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dto))
            {
                date = DateOnly.FromDateTime(dto.DateTime);
                return true;
            }

            if (DateTime.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None, out var fallback))
            {
                date = DateOnly.FromDateTime(fallback);
                return true;
            }
        }

        date = default;
        return false;
    }

    /// <summary>
    /// Valida el formato estricto de <c>paymentTime</c>: <c>HH:mm:ss</c> o <c>HH:mm</c>.
    /// </summary>
    public static bool IsValidTimeFormat(string? value)
    {
        var trimmed = (value ?? string.Empty).Trim();

        return TimeOnly.TryParseExact(trimmed, TimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out _)
            || TimeOnly.TryParseExact(trimmed, ShortTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
    }
}
