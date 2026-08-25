using Domain.Entities;
using FluentAssertions;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Payments.Tests;

[TestClass]
public sealed class PaymentsDbContextTests
{
    [TestMethod]
    public void Model_RequiresIdentifiers_UsesUniqueIndexesAndUtcColumns()
    {
        var options = new DbContextOptionsBuilder<PaymentsDbContext>()
            .UseNpgsql("Host=localhost;Database=cospail_payments_model_test;Username=postgres;Password=unused")
            .Options;
        using var context = new PaymentsDbContext(options);
        var entityType = context.Model.FindEntityType(typeof(PagoQr));

        entityType.Should().NotBeNull();
        entityType!.FindProperty(nameof(PagoQr.TransactionId))!.IsNullable.Should().BeFalse();
        entityType.FindProperty(nameof(PagoQr.QrId))!.IsNullable.Should().BeFalse();
        entityType.FindProperty(nameof(PagoQr.CreatedAtUtc))!.GetColumnType().Should().Be("timestamp with time zone");
        entityType.FindProperty(nameof(PagoQr.PaidAtUtc))!.GetColumnType().Should().Be("timestamp with time zone");
        entityType.GetIndexes().Should().Contain(x => x.IsUnique && x.Properties.Single().Name == nameof(PagoQr.TransactionId));
        entityType.GetIndexes().Should().Contain(x => x.IsUnique && x.Properties.Single().Name == nameof(PagoQr.QrId));
    }

    [TestMethod]
    public void Model_MapsCospailPaymentTables()
    {
        var options = new DbContextOptionsBuilder<PaymentsDbContext>()
            .UseNpgsql("Host=localhost;Database=cospail_payments_model_test;Username=postgres;Password=unused")
            .Options;
        using var context = new PaymentsDbContext(options);

        var pagoCospail = context.Model.FindEntityType(typeof(PagoCospail));
        pagoCospail.Should().NotBeNull();
        pagoCospail!.GetTableName().Should().Be("pagos_cospail");
        pagoCospail!.GetIndexes().Should().Contain(x =>
            x.IsUnique && x.Properties.Single().Name == nameof(PagoCospail.PagoQrId));
        pagoCospail!.FindProperty(nameof(PagoCospail.CreatedAtUtc))!.GetColumnType().Should().Be("timestamp with time zone");

        var deuda = context.Model.FindEntityType(typeof(DeudaCospail));
        deuda.Should().NotBeNull();
        deuda!.GetTableName().Should().Be("deudas_cospail");
        deuda!.FindProperty(nameof(DeudaCospail.PagoCospailId))!.IsNullable.Should().BeFalse();
        deuda!.GetForeignKeys().Should().Contain(x =>
            x.PrincipalEntityType.ClrType == typeof(PagoCospail));
        deuda!.GetIndexes().Should().Contain(x =>
            x.Properties
                .Select(p => p.Name)
                .SequenceEqual(new[]
                {
                    nameof(DeudaCospail.FixedCode),
                    nameof(DeudaCospail.CreditNumber),
                    nameof(DeudaCospail.Type),
                    nameof(DeudaCospail.Status)
                }));
    }

    [TestMethod]
    public void Model_MapsPagoQrNotificationsTable()
    {
        var options = new DbContextOptionsBuilder<PaymentsDbContext>()
            .UseNpgsql("Host=localhost;Database=cospail_payments_model_test;Username=postgres;Password=unused")
            .Options;
        using var context = new PaymentsDbContext(options);

        var notificacion = context.Model.FindEntityType(typeof(NotificacionPagoQr));
        notificacion.Should().NotBeNull();
        notificacion!.GetTableName().Should().Be("notificaciones_pago_qr");
        notificacion!.FindProperty(nameof(NotificacionPagoQr.PagoQrId))!.IsNullable.Should().BeFalse();
        notificacion!.FindProperty(nameof(NotificacionPagoQr.SenderName))!.GetMaxLength().Should().Be(200);
        notificacion!.FindProperty(nameof(NotificacionPagoQr.PaymentAtUtc))!.GetColumnType().Should().Be("timestamp with time zone");
        notificacion!.FindProperty(nameof(NotificacionPagoQr.ReceivedAtUtc))!.GetColumnType().Should().Be("timestamp with time zone");
        notificacion!.GetForeignKeys().Should().Contain(x =>
            x.PrincipalEntityType.ClrType == typeof(PagoQr));
        notificacion!.GetIndexes().Should().Contain(x =>
            x.Properties.Single().Name == nameof(NotificacionPagoQr.PagoQrId));
    }

    [TestMethod]
    public async Task PostgreSql_MigrationAndPersistence_WorkWhenConnectionIsConfigured()
    {
        var connectionString = Environment.GetEnvironmentVariable("PAYMENTS_TEST_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Assert.Inconclusive("Configure PAYMENTS_TEST_CONNECTION_STRING to run the PostgreSQL integration test.");
        }

        var options = new DbContextOptionsBuilder<PaymentsDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        await using var context = new PaymentsDbContext(options);
        await context.Database.MigrateAsync();

        var transactionId = $"test-{Guid.NewGuid():N}";
        var pagoQr = new PagoQr(
            transactionId, $"qr-{Guid.NewGuid():N}", 1.20m, "BOB", DateOnly.FromDateTime(DateTime.UtcNow), true,
            false, "Prueba de integración", "001", null, DateTime.UtcNow);

        await context.PagosQr.AddAsync(pagoQr);
        await context.SaveChangesAsync();

        var stored = await context.PagosQr.SingleAsync(x => x.TransactionId == transactionId);
        stored.Status.Should().Be(PagoQrStatus.Pendiente);
        stored.CreatedAtUtc.Kind.Should().Be(DateTimeKind.Utc);

        //context.PagosQr.Remove(stored);
        //await context.SaveChangesAsync();
    }
}
