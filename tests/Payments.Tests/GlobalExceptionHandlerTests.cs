using System.Text.Json;
using Api.Middleware;
using Application.Common.Exceptions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Payments.Tests;

[TestClass]
public sealed class GlobalExceptionHandlerTests
{
    private static GlobalExceptionHandler CreateHandler(RequestDelegate next) =>
        new(next, NullLogger<GlobalExceptionHandler>.Instance);

    private static async Task<JsonDocument> InvokeAsync(RequestDelegate next)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await CreateHandler(next).InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        return await JsonDocument.ParseAsync(context.Response.Body);
    }

    [TestMethod]
    public async Task Invoke_WhenBankOperationException_Returns502WithBankMessage()
    {
        const string bankMessage = "Banco Económico rechazó la generación del QR. Código: 500, Mensaje: Estamos realizando un mantenimiento.";

        var doc = await InvokeAsync(_ => throw new BankOperationException(500, bankMessage));

        // DefaultHttpContext sin Response.StatusCode seteado... el middleware lo setea:
        doc.RootElement.GetProperty("title").GetString().Should().Be("Error del banco");
        doc.RootElement.GetProperty("detail").GetString().Should().Be(bankMessage);
    }

    [TestMethod]
    public async Task Invoke_WhenBankOperationException_Sets502StatusCode()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await CreateHandler(_ => throw new BankOperationException(500, "Banco Económico rechazó la generación del QR."))
            .InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status502BadGateway);
    }

    [TestMethod]
    public async Task Invoke_WhenUnexpectedException_Returns500GenericWithoutLeakingDetails()
    {
        var doc = await InvokeAsync(_ => throw new InvalidOperationException(" StackTrace secreto con passwords "));

        doc.RootElement.GetProperty("title").GetString().Should().Be("Internal Server Error");
        doc.RootElement.GetProperty("detail").GetString().Should().Be("An unexpected error occurred.");
    }
}
