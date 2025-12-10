using Microsoft.EntityFrameworkCore.Migrations;

namespace FusionComms.Migrations
{
    public partial class AddDiscount : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "WhatsAppOrderSessions",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "DiscountCode",
                table: "WhatsAppOrderSessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DiscountType",
                table: "WhatsAppOrderSessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountValue",
                table: "WhatsAppOrderSessions",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "WhatsAppOrderSessions");

            migrationBuilder.DropColumn(
                name: "DiscountCode",
                table: "WhatsAppOrderSessions");

            migrationBuilder.DropColumn(
                name: "DiscountType",
                table: "WhatsAppOrderSessions");

            migrationBuilder.DropColumn(
                name: "DiscountValue",
                table: "WhatsAppOrderSessions");
        }
    }
}
