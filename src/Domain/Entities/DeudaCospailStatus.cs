namespace Domain.Entities;

/// <summary>
/// Estados admitidos para una deuda individual de Cospail dentro de un pago.
/// </summary>
public enum DeudaCospailStatus
{
    /// <summary>
    /// La deuda está asociada a un pago y todavía no fue pagada ni registrada.
    /// </summary>
    Pendiente = 0,

    /// <summary>
    /// Banco Económico notificó el pago del QR que incluía esta deuda.
    /// </summary>
    Pagado = 1,

    /// <summary>
    /// La deuda fue registrada en Cospail mediante grabarCobrosWEB.
    /// </summary>
    CospailRegistrado = 2,

    /// <summary>
    /// El QR que incluía esta deuda fue anulado ante Banco Económico. La deuda
    /// sigue debiéndose en Cospail; puede incluirse en un nuevo pago.
    /// </summary>
    Anulado = 3
}