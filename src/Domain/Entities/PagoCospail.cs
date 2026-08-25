namespace Domain.Entities;

/// <summary>
/// Representa un pago agrupado de una a varias deudas de Cospail que se cobran
/// juntas mediante un único QR de Banco Económico.
/// </summary>
public sealed class PagoCospail
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
    /// Suma de los importes de las deudas incluidas en el pago.
    /// </summary>
    public decimal TotalAmount { get; private set; }

    /// <summary>
    /// Estado actual del pago.
    /// </summary>
    public PagoCospailStatus Status { get; private set; }

    /// <summary>
    /// Identificador del QR emitido por Banco Económico asociado al pago.
    /// </summary>
    public Guid? PagoQrId { get; private set; }

    /// <summary>
    /// QR de Banco Económico asociado al pago, cuando fue generado.
    /// </summary>
    public PagoQr? Qr { get; private set; }

    /// <summary>
    /// Fecha y hora UTC en que se creó el pago.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Fecha y hora UTC de la última transición de estado.
    /// </summary>
    public DateTime? UpdatedAtUtc { get; private set; }

    /// <summary>
    /// Deudas incluidas en el pago.
    /// </summary>
    public ICollection<DeudaCospail> Deudas { get; private set; } = new List<DeudaCospail>();

    private PagoCospail()
    {
    }

    /// <summary>
    /// Crea un pago pendiente con las deudas seleccionadas por el socio.
    /// </summary>
    public PagoCospail(
        int fixedCode,
        string documentId,
        string? memberName,
        decimal totalAmount,
        DateTime createdAtUtc
    )
    {
        Id = Guid.NewGuid();
        FixedCode = fixedCode;
        DocumentId = documentId;
        MemberName = memberName;
        TotalAmount = totalAmount;
        CreatedAtUtc = DateTime.SpecifyKind(createdAtUtc, DateTimeKind.Utc);
        Status = PagoCospailStatus.Pendiente;
    }

    /// <summary>
    /// Asocia una deuda al pago.
    /// </summary>
    public void AddDeuda(DeudaCospail deuda)
    {
        Deudas.Add(deuda);
    }

    /// <summary>
    /// Asocia el QR emitido por Banco Económico al pago. Solo es válido cuando
    /// el pago todavía está pendiente.
    /// </summary>
    /// <param name="pagoQrId">Identificador del QR emitido.</param>
    /// <returns><see langword="true"/> si se produjo la transición de estado.</returns>
    public bool MarkAsQrGenerated(Guid pagoQrId)
    {
        if (Status != PagoCospailStatus.Pendiente)
        {
            return false;
        }

        PagoQrId = pagoQrId;
        Status = PagoCospailStatus.QRGenerado;
        Touch();
        return true;
    }

    /// <summary>
    /// Marca el pago como anulado cuando su QR fue anulado ante Banco Económico.
    /// Es un estado terminal: para pagar de nuevo hay que iniciar un nuevo pago.
    /// </summary>
    /// <returns><see langword="true"/> si se produjo la transición de estado.</returns>
    public bool MarkAsAnulado()
    {
        if (Status != PagoCospailStatus.QRGenerado)
        {
            return false;
        }

        Status = PagoCospailStatus.Anulado;
        Touch();
        return true;
    }

    /// <summary>
    /// Marca el pago como pagado según la notificación de Banco Económico. La
    /// operación es idempotente para un pago ya registrado en Cospail.
    /// </summary>
    /// <returns><see langword="true"/> si se produjo la transición de estado.</returns>
    public bool MarkAsPagado()
    {
        if (Status == PagoCospailStatus.CospailRegistrado)
        {
            return false;
        }

        Status = PagoCospailStatus.Pagado;
        Touch();
        return true;
    }

    /// <summary>
    /// Marca el pago como registrado en Cospail una vez que todas sus deudas
    /// fueron registradas mediante grabarCobrosWEB.
    /// </summary>
    /// <returns><see langword="true"/> si se produjo la transición de estado.</returns>
    public bool MarkAsCospailRegistrado()
    {
        if (Status == PagoCospailStatus.CospailRegistrado)
        {
            return false;
        }

        Status = PagoCospailStatus.CospailRegistrado;
        Touch();
        return true;
    }

    private void Touch()
    {
        UpdatedAtUtc = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
    }
}