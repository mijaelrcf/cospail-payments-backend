namespace Application.DTOs.BancoEconomico.Responses;

using System.Text.Json.Serialization;

/// <summary>
/// Respuesta de consulta de movimientos de cuenta (8.1 queryMovements).
/// </summary>
public sealed class QueryMovementsResponseDto
{
    public int ResponseCode { get; set; }
    public string? Message { get; set; }
    public AccountHeaderDto? AccountHeader { get; set; }
    public List<AccountDetailDto> AccountDetailList { get; set; } = [];
    public List<AccountWithheldDto> AccountWithheldList { get; set; } = [];

    /// <summary>
    /// Información general de la cuenta (objeto AccountHeader del Anexo 1).
    /// </summary>
    public sealed class AccountHeaderDto
    {
        public string? AccountCode { get; set; }
        public string? AccountTypeCode { get; set; }
        public string? ProductName { get; set; }
        public string? Status { get; set; }
        public string? Currency { get; set; }
        public decimal Balance { get; set; }
        public decimal BalanceReserved { get; set; }
        public decimal BalanceRetained { get; set; }
        public decimal BalanceAvailable { get; set; }
    }

    /// <summary>
    /// Movimiento de cuenta (objeto AccountDetail del Anexo 1).
    /// </summary>
    public sealed class AccountDetailDto
    {
        [JsonConverter(typeof(LenientInt64Converter))]
        public long TransactionId { get; set; }
        public string? Date { get; set; }
        public string? Time { get; set; }
        [JsonConverter(typeof(LenientInt64Converter))]
        public long DocumentNumber { get; set; }
        public string? TransactionType { get; set; }
        public decimal Amount { get; set; }
        public string? Description { get; set; }
        public string? ClienteNote { get; set; }
    }

    /// <summary>
    /// Retención en cuenta (objeto AccountWithheld del Anexo 1).
    /// </summary>
    public sealed class AccountWithheldDto
    {
        [JsonConverter(typeof(LenientInt64Converter))]
        public long TransactionId { get; set; }
        public string? Date { get; set; }
        public string? Time { get; set; }
        public decimal Amount { get; set; }
        public string? Description { get; set; }
        public string? Instruction { get; set; }
        public string? Demanding { get; set; }
        public string? Judge { get; set; }
        public string? Piet { get; set; }
    }
}
