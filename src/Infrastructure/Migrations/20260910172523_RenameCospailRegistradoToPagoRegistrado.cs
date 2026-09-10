using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameCospailRegistradoToPagoRegistrado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Los estados se persisten como string (HasConversion<string>).
            // El renombrado del enum no cambia el modelo, pero las filas
            // existentes conservan el valor anterior y deben migrarse.
            migrationBuilder.Sql(
                "UPDATE pagos_cospail SET status = 'PagoRegistrado' WHERE status = 'CospailRegistrado';");
            migrationBuilder.Sql(
                "UPDATE deudas_cospail SET status = 'PagoRegistrado' WHERE status = 'CospailRegistrado';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE deudas_cospail SET status = 'CospailRegistrado' WHERE status = 'PagoRegistrado';");
            migrationBuilder.Sql(
                "UPDATE pagos_cospail SET status = 'CospailRegistrado' WHERE status = 'PagoRegistrado';");
        }
    }
}
