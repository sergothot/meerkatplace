using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OrderService.API.Infrastructure.Persistence;

#nullable disable

namespace OrderService.API.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(OrderDbContext))]
    [Migration("20260621170500_AddOrderInboxProcessedMessages")]
    public partial class AddOrderInboxProcessedMessages : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "IX_ProcessedIntegrationMessages_MessageId",
                table: "ProcessedIntegrationMessages",
                column: "MessageId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProcessedIntegrationMessages");
        }
    }
}