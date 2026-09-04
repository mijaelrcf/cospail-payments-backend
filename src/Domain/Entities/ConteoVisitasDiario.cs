namespace Domain.Entities;

/// <summary>
/// Contador agregado de visitas (pageviews) del frontend cliente por día
/// calendario de Bolivia. Contador puro: no guarda datos de visitantes
/// (sin IP, sin User-Agent, sin cookies) para que la tabla no crezca
/// (365 filas/año) y no haya PII que retener.
/// </summary>
public sealed class ConteoVisitasDiario
{
    /// <summary>
    /// Fecha calendario en Bolivia (America/La_Paz). Clave primaria.
    /// </summary>
    public DateOnly Fecha { get; private set; }

    /// <summary>
    /// Total de visitas registradas ese día.
    /// </summary>
    public int TotalVisitas { get; private set; }

    /// <summary>
    /// Última actualización en UTC.
    /// </summary>
    public DateTime UpdatedAtUtc { get; private set; }

    private ConteoVisitasDiario()
    {
    }

    public ConteoVisitasDiario(DateOnly fecha)
    {
        Fecha = fecha;
        TotalVisitas = 0;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Incrementa el contador en uno.
    /// </summary>
    public void Increment()
    {
        TotalVisitas++;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
