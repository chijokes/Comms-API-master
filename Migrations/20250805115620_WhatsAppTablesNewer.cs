using Microsoft.EntityFrameworkCore.Migrations;

namespace FusionComms.Migrations
{
    public partial class WhatsAppTablesNewer : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RevenueCenterId",
                table: "WhatsAppBusinesses");

            migrationBuilder.AddColumn<string>(
                name: "RevenueCenterId",
                table: "WhatsAppProducts",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "OrderSessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RevenueCenterId",
                table: "OrderSessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TaxExclusive",
                table: "OrderSessions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "Charge",
                table: "Orders",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "RevenueCenterId",
                table: "Orders",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RevenueCenterId",
                table: "WhatsAppProducts");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "OrderSessions");

            migrationBuilder.DropColumn(
                name: "RevenueCenterId",
                table: "OrderSessions");

            migrationBuilder.DropColumn(
                name: "TaxExclusive",
                table: "OrderSessions");

            migrationBuilder.DropColumn(
                name: "Charge",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RevenueCenterId",
                table: "Orders");

            migrationBuilder.AddColumn<string>(
                name: "RevenueCenterId",
                table: "WhatsAppBusinesses",
                type: "text",
                nullable: true);
        }
    }
}
