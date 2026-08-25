namespace Domain.Entities;

/// <summary>
/// Representa una deuda específica de un socio de Cospail incluida en un pago.
/// Almacena una instantánea de la deuda devuelta por el servicio SOAP.
/// </summary>
public sealed class DeudaCospail
{
    /// <summary>
    /// Identificador interno del registro.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Código fijo del socio en Cospail.
    /// </summary>
    public int FixedCode { get; private set; }

    /// <summary>
    /// Documento de identidad o NIT del socio.
    /// </summary>
    public string DocumentId { get; private set; } = string.Empty;

    /// <summary>
    /// Nombre del socio, cuando Cospail lo entrega.
    /// </summary>
    public string? MemberName { get; private set; }

    /// <summary>
    /// Número de crédito de la deuda en Cospail.
    /// </summary>
    public int CreditNumber { get; private set; }

    /// <summary>
    /// Tipo de deuda en Cospail.
    /// </summary>
    public int Type { get; private set; }

    /// <summary>
    /// Número de aviso de la deuda.
    /// </summary>
    public int NoticeNumber { get; private set; }

    /// <summary>
    /// Año del período al que corresponde la deuda.
    /// </summary>
    public int Year { get; private set; }

    /// <summary>
    /// Mes del período al que corresponde la deuda.
    /// </summary>
    public int Month { get; private set; }

    /// <summary>
    /// Período de la deuda tal como lo reporta Cospail.
    /// </summary>
    public string Period { get; private set; } = string.Empty;

    /// <summary>
    /// Importe adeudado.
    /// </summary>
    public decimal Amount { get; private set; }

    /// <summary>
    /// Estado actual de la deuda dentro del pago.
    /// </summary>
    public DeudaCospailStatus Status { get; private set; }

    /// <summary>
    /// Identificador del pago al que pertenece la deuda.
    /// </summary>
    public Guid PagoCospailId { get; private set; }

    /// <summary>
    /// Pago al que pertenece la deuda.
    /// </summary>
    public PagoCospail PagoCospail { get; private set; } = null!;

    private DeudaCospail()
    {
    }

    /// <summary>
    /// Crea una deuda pendiente a partir de la información devuelta por Cospail.
    /// </summary>
    public DeudaCospail(
        int fixedCode,
        string documentId,
        string? memberName,
        int creditNumber,
        int type,
        int noticeNumber,
        int year,
        int month,
        string period,
        decimal amount
    )
    {
        Id = Guid.NewGuid();
        FixedCode = fixedCode;
        DocumentId = documentId;
        MemberName = memberName;
        CreditNumber = creditNumber;
        Type = type;
        NoticeNumber = noticeNumber;
        Year = year;
        Month = month;
        Period = period;
        Amount = amount;
        Status = DeudaCospailStatus.Pendiente;
    }

    /// <summary>
    /// Marca la deuda como pagada según la notificación de Banco Económico.
    /// </summary>
    /// <returns><see langword="true"/> si se produjo la transición de estado.</returns>
    public bool MarkAsPagado()
    {
        if (Status == DeudaCospailStatus.CospailRegistrado)
        {
            return false;
        }

        Status = DeudaCospailStatus.Pagado;
        return true;
    }

    /// <summary>
    /// Marca la deuda como registrada en Cospail mediante grabarCobrosWEB.
    /// </summary>
    /// <returns><see langword="true"/> si se produjo la transición de estado.</returns>
    public bool MarkAsCospailRegistrado()
    {
        if (Status == DeudaCospailStatus.CospailRegistrado)
        {
            return false;
        }

        Status = DeudaCospailStatus.CospailRegistrado;
        return true;
    }

    /// <summary>
    /// Marca la deuda como anulado junto con el pago cuyo QR fue anulado.
    /// Solo es válido para deudas pendientes; la deuda sigue debiéndose en Cospail.
    /// </summary>
    /// <returns><see langword="true"/> si se produjo la transición de estado.</returns>
    public bool MarkAsAnulado()
    {
        if (Status != DeudaCospailStatus.Pendiente)
        {
            return false;
        }

        Status = DeudaCospailStatus.Anulado;
        return true;
    }
}