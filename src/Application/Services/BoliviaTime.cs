using Application.Common;

namespace Application.Services;

/// <summary>
/// Utilidades para convertir instantes UTC a la hora local de Bolivia (UTC-04:00).
/// </summary>
internal static class BoliviaTime
{
    private const string TimeZoneId = "America/La_Paz";
    private static readonly TimeZoneInfo TimeZone = GetBoliviaTimeZone();

    /// <summary>
    /// Convierte una fecha y hora UTC a la hora local de Bolivia.
    /// </summary>
    internal static DateTime FromUtc(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZone);

    /// <summary>
    /// Fecha actual en Bolivia.
    /// </summary>
    internal static DateOnly Today() => DateOnly.FromDateTime(FromUtc(DateTime.UtcNow));

    private static TimeZoneInfo GetBoliviaTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return TimeZoneInfo.CreateCustomTimeZone("Bolivia", PaymentDateTime.BoliviaUtcOffset, "Bolivia", "Bolivia");
        }
    }

    /// <summary>
    /// Convierte un rango de fechas Bolivia [desde, hasta] inclusivo a rango UTC
    /// [inicio, finExclusivo). Bolivia es UTC-04:00 fijo (sin DST):
    /// medianoche Bolivia = 04:00 UTC del mismo día.
    /// </summary>
    internal static (DateTime DesdeUtc, DateTime HastaUtcExclusivo) ToUtcRange(DateOnly desde, DateOnly hasta)
    {
        // Medianoche Bolivia = 04:00 UTC = medianoche UTC menos el offset (UTC-04:00).
        var desdeUtc = new DateTime(desde.Year, desde.Month, desde.Day, 0, 0, 0, DateTimeKind.Utc).Add(-PaymentDateTime.BoliviaUtcOffset);
        var hastaUtcExclusivo = new DateTime(hasta.Year, hasta.Month, hasta.Day, 0, 0, 0, DateTimeKind.Utc).Add(-PaymentDateTime.BoliviaUtcOffset).AddDays(1);
        return (desdeUtc, hastaUtcExclusivo);
    }
}
