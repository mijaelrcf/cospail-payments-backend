namespace Domain.Entities;

/// <summary>
/// Estados admitidos para un cobro QR gestionado por Banco Económico.
/// </summary>
public enum PagoQrStatus
{
    /// <summary>
    /// El QR fue emitido y todavía no se recibió un pago confirmado.
    /// </summary>
    Pendiente = 0,

    /// <summary>
    /// Banco Económico notificó el pago del QR.
    /// </summary>
    Pagado = 1,

    /// <summary>
    /// El QR fue anulado ante Banco Económico y ya no puede pagarse.
    /// </summary>
    Anulado = 2
}
