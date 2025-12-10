using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FusionComms.Migrations
{
    public partial class AddWhatsAppTablesOthers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "SupportsChat",
                table: "WhatsAppBusinesses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "CustomerName",
                table: "OrderSessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryChargeId",
                table: "OrderSessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerName",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CustomerEmails",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    BusinessId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PhoneNumber = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    CustomerName = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerEmails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerEmails_WhatsAppBusinesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "WhatsAppBusinesses",
                        principalColumn: "BusinessId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerEmails_BusinessId",
                table: "CustomerEmails",
                column: "BusinessId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerEmails");

            migrationBuilder.DropColumn(
                name: "SupportsChat",
                table: "WhatsAppBusinesses");

            migrationBuilder.DropColumn(
                name: "CustomerName",
                table: "OrderSessions");

            migrationBuilder.DropColumn(
                name: "DeliveryChargeId",
                table: "OrderSessions");

            migrationBuilder.DropColumn(
                name: "CustomerName",
                table: "Orders");
        }
    }
}
