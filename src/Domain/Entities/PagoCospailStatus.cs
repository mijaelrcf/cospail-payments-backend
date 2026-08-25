namespace Domain.Entities;

/// <summary>
/// Estados admitidos para un pago agrupado de deudas de Cospail.
/// </summary>
public enum PagoCospailStatus
{
    /// <summary>
    /// El pago fue creado con sus deudas pendientes y aún no tiene un QR asociado.
    /// </summary>
    Pendiente = 0,

    /// <summary>
    /// Banco Económico emitió el QR asociado al pago y se espera el pago.
    /// </summary>
    QRGenerado = 1,

    /// <summary>
    /// Banco Económico notificó el pago del QR. El cobro todavía no fue registrado
    /// íntegramente en Cospail (o quedó pendiente de registro para reintento).
    /// </summary>
    Pagado = 2,

    /// <summary>
    /// Todas las deudas del pago fueron registradas en Cospail mediante grabarCobrosWEB.
    /// </summary>
    CospailRegistrado = 3,

    /// <summary>
    /// El QR asociado al pago fue anulado ante Banco Económico. Estado terminal:
    /// para pagar de nuevo hay que iniciar un nuevo pago.
    /// </summary>
    Anulado = 4
}