using Microsoft.EntityFrameworkCore.Migrations;

namespace FusionComms.Migrations
{
    public partial class AddPackagingAndTopping : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CurrentPackId",
                table: "WhatsAppOrderSessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PendingToppings",
                table: "WhatsAppOrderSessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PendingToppingsQueue",
                table: "WhatsAppOrderSessions",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentPackId",
                table: "WhatsAppOrderSessions");

            migrationBuilder.DropColumn(
                name: "PendingToppings",
                table: "WhatsAppOrderSessions");

            migrationBuilder.DropColumn(
                name: "PendingToppingsQueue",
                table: "WhatsAppOrderSessions");
        }
    }
}
