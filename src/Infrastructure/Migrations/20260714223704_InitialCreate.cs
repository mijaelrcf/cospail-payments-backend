using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pagos_qr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    transaction_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    qr_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    single_use = table.Column<bool>(type: "boolean", nullable: false),
                    modify_amount = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    branch_code = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    paid_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pagos_qr", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_pagos_qr_created_at_utc",
                table: "pagos_qr",
                column: "created_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_pagos_qr_qr_id",
                table: "pagos_qr",
                column: "qr_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pagos_qr_status",
                table: "pagos_qr",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_pagos_qr_transaction_id",
                table: "pagos_qr",
                column: "transaction_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pagos_qr");
        }
    }
}
