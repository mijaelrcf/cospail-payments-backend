using System.Text.Json;
using Application.DTOs.BancoEconomico.Responses;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Payments.Tests;

[TestClass]
public sealed class QueryMovementsDeserializationTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [TestMethod]
    public void Deserialize_WhenBankSendsDecimalDocumentNumber_ParsesSuccessfully()
    {
        const string payload = """
            {
                "accountHeader": {
                    "accountCode": "1061602532",
                    "accountTypeCode": "CA",
                    "productName": "BASICA",
                    "status": "ACTIVA",
                    "currency": "BOB",
                    "balance": 273022.41,
                    "balanceReserved": 24460.16,
                    "balanceRetained": 0.0,
                    "balanceAvailable": 273020.35
                },
                "accountDetailList": [
                    {
                        "transactionId": 900833955,
                        "date": "2026-09-09",
                        "time": "11:48:41",
                        "documentNumber": 18459521.0,
                        "transactionType": "C",
                        "amount": -1.00,
                        "description": "TRASPASO CA/CC CON QR (MOVIL)",
                        "clienteNote": "TRASP.CTAS.TERCEROS APE1-190439 APE2-190439 NOMB-190439"
                    }
                ],
                "accountWithheldList": [],
                "responseCode": 0,
                "message": ""
            }
            """;

        var result = JsonSerializer.Deserialize<QueryMovementsResponseDto>(payload, Options);

        result.Should().NotBeNull();
        result!.ResponseCode.Should().Be(0);
        result.AccountHeader!.AccountCode.Should().Be("1061602532");
        result.AccountHeader.Balance.Should().Be(273022.41m);
        result.AccountDetailList.Should().HaveCount(1);
        result.AccountDetailList[0].TransactionId.Should().Be(900833955);
        result.AccountDetailList[0].DocumentNumber.Should().Be(18459521);
        result.AccountDetailList[0].Amount.Should().Be(-1.00m);
    }
}
