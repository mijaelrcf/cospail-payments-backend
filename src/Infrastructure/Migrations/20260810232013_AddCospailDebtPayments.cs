using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCospailDebtPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pagos_cospail",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fixed_code = table.Column<int>(type: "integer", nullable: false),
                    document_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    member_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    pago_qr_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pagos_cospail", x => x.id);
                    table.ForeignKey(
                        name: "FK_pagos_cospail_pagos_qr_pago_qr_id",
                        column: x => x.pago_qr_id,
                        principalTable: "pagos_qr",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "deudas_cospail",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fixed_code = table.Column<int>(type: "integer", nullable: false),
                    document_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    member_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    credit_number = table.Column<int>(type: "integer", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    notice_number = table.Column<int>(type: "integer", nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    month = table.Column<int>(type: "integer", nullable: false),
                    period = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    pago_cospail_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_deudas_cospail", x => x.id);
                    table.ForeignKey(
                        name: "FK_deudas_cospail_pagos_cospail_pago_cospail_id",
                        column: x => x.pago_cospail_id,
                        principalTable: "pagos_cospail",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_deudas_cospail_fixed_code_credit_number_type_status",
                table: "deudas_cospail",
                columns: new[] { "fixed_code", "credit_number", "type", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_deudas_cospail_pago_cospail_id",
                table: "deudas_cospail",
                column: "pago_cospail_id");

            migrationBuilder.CreateIndex(
                name: "IX_pagos_cospail_fixed_code",
                table: "pagos_cospail",
                column: "fixed_code");

            migrationBuilder.CreateIndex(
                name: "IX_pagos_cospail_pago_qr_id",
                table: "pagos_cospail",
                column: "pago_qr_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pagos_cospail_status",
                table: "pagos_cospail",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "deudas_cospail");

            migrationBuilder.DropTable(
                name: "pagos_cospail");
        }
    }
}
