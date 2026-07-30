using Application.DTOs.Cospail.Requests;
using Application.DTOs.Cospail.Responses;
using Application.Interfaces.Internal;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CospailSoapController : ControllerBase
{
    private readonly ICospailSoapService _cospailService;

    public CospailSoapController(ICospailSoapService cospailService)
    {
        _cospailService = cospailService;
    }

    /// <summary>
    /// Consulta la deuda de un socio mediante código fijo.
    /// </summary>
    /// <param name="fixedCode">Código fijo del socio.</param>
    [HttpGet("debt/{fixedCode:int}")]
    [ProducesResponseType(typeof(CospailDebtResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDebt(int fixedCode, CancellationToken cancellationToken)
    {
        var result = await _cospailService.GetDebtAsync(fixedCode, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Consulta la deuda de un socio mediante código fijo y documento de identidad o NIT.
    /// </summary>
    /// <param name="fixedCode">Código fijo del socio.</param>
    /// <param name="documentId">Documento de Identidad o NIT.</param>
    [HttpGet("member-debt-by-document")]
    [ProducesResponseType(typeof(GetMemberDebtByDocumentResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMemberDebtByDocument(
        [FromQuery] int fixedCode,
        [FromQuery] string documentId,
        CancellationToken cancellationToken
    )
    {
        if (fixedCode <= 0)
        {
            return BadRequest("fixedCode debe ser mayor a cero.");
        }

        if (string.IsNullOrWhiteSpace(documentId))
        {
            return BadRequest("documentId es requerido.");
        }

        var result = await _cospailService.GetMemberDebtByDocumentAsync(
            fixedCode,
            documentId,
            cancellationToken
        );

        return Ok(result);
    }

    /// <summary>
    /// Consulta la deuda de un socio mediante código fijo y documento de identidad o NIT.
    /// </summary>
    /// <param name="fixedCode">Código fijo del socio.</param>
    /// <param name="documentId">Documento de Identidad o NIT.</param>
    [HttpPost("payments/confirm")]
    [ProducesResponseType(typeof(ConfirmPaymentResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ConfirmPayment(
        [FromBody] ConfirmPaymentRequestDto request,
        CancellationToken cancellationToken
    )
    {
        var result = await _cospailService.ConfirmPaymentAsync(request, cancellationToken);

        return Ok(result);
    }
}
