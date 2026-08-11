using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPagoQrNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "notificaciones_pago_qr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    pago_qr_id = table.Column<Guid>(type: "uuid", nullable: false),
                    qr_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    transaction_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    payment_date = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    payment_time = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    payment_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    sender_bank_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    sender_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    sender_document_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    sender_account = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    branch_code = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    received_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notificaciones_pago_qr", x => x.id);
                    table.ForeignKey(
                        name: "FK_notificaciones_pago_qr_pagos_qr_pago_qr_id",
                        column: x => x.pago_qr_id,
                        principalTable: "pagos_qr",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_notificaciones_pago_qr_pago_qr_id",
                table: "notificaciones_pago_qr",
                column: "pago_qr_id");

            migrationBuilder.CreateIndex(
                name: "IX_notificaciones_pago_qr_qr_id",
                table: "notificaciones_pago_qr",
                column: "qr_id");

            migrationBuilder.CreateIndex(
                name: "IX_notificaciones_pago_qr_received_at_utc",
                table: "notificaciones_pago_qr",
                column: "received_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_notificaciones_pago_qr_transaction_id",
                table: "notificaciones_pago_qr",
                column: "transaction_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notificaciones_pago_qr");
        }
    }
}

