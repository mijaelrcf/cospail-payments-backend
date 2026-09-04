namespace Application.DTOs.Admin.Responses;

/// <summary>
/// Métricas de un periodo (día, mes o año): ingresos vs QR generados vs pagados.
/// </summary>
public sealed class AnalyticsPeriodDto
{
    public int Ingresos { get; init; }
    public int QrGenerados { get; init; }
    public int Pagados { get; init; }

    /// <summary>
    /// Conversión QR -&gt; pago (0..1). 0 cuando no hay QR generados.
    /// </summary>
    public double ConversionQrAPago { get; init; }
}
