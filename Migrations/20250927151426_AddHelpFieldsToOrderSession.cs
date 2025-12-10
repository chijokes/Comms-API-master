using Microsoft.EntityFrameworkCore.Migrations;

namespace FusionComms.Migrations
{
    public partial class AddHelpFieldsToOrderSession : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HelpEmail",
                table: "WhatsAppBusinesses");

            migrationBuilder.DropColumn(
                name: "HelpPhone",
                table: "WhatsAppBusinesses");

            migrationBuilder.AddColumn<string>(
                name: "HelpEmail",
                table: "WhatsAppOrderSessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HelpPhoneNumber",
                table: "WhatsAppOrderSessions",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HelpEmail",
                table: "WhatsAppOrderSessions");

            migrationBuilder.DropColumn(
                name: "HelpPhoneNumber",
                table: "WhatsAppOrderSessions");

            migrationBuilder.AddColumn<string>(
                name: "HelpEmail",
                table: "WhatsAppBusinesses",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HelpPhone",
                table: "WhatsAppBusinesses",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }
    }
}
