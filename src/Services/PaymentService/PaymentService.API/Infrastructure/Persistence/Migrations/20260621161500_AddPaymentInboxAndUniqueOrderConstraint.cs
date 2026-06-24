using System;
using PaymentService.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PaymentService.API.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(PaymentDbContext))]
    [Migration("20260621161500_AddPaymentInboxAndUniqueOrderConstraint")]
    public partial class AddPaymentInboxAndUniqueOrderConstraint : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PaymentTransactions_OrderId",
                table: "PaymentTransactions");

            migrationBuilder.CreateTable(
                name: "ProcessedIntegrationMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Consumer = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedIntegrationMessages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_OrderId",
                table: "PaymentTransactions",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedIntegrationMessages_MessageId",
                table: "ProcessedIntegrationMessages",
                column: "MessageId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProcessedIntegrationMessages");

            migrationBuilder.DropIndex(
                name: "IX_PaymentTransactions_OrderId",
                table: "PaymentTransactions");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_OrderId",
                table: "PaymentTransactions",
                column: "OrderId");
        }
    }
}