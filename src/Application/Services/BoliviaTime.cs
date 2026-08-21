namespace Application.Services;

/// <summary>
/// Utilidades para convertir instantes UTC a la hora local de Bolivia (UTC-04:00).
/// </summary>
internal static class BoliviaTime
{
    private static readonly TimeZoneInfo TimeZone = GetBoliviaTimeZone();

    /// <summary>
    /// Convierte una fecha y hora UTC a la hora local de Bolivia.
    /// </summary>
    internal static DateTime FromUtc(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZone);

    private static TimeZoneInfo GetBoliviaTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/La_Paz");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.CreateCustomTimeZone("Bolivia", TimeSpan.FromHours(-4), "Bolivia", "Bolivia");
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.CreateCustomTimeZone("Bolivia", TimeSpan.FromHours(-4), "Bolivia", "Bolivia");
        }
    }
}
