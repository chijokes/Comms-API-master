using Microsoft.EntityFrameworkCore.Migrations;

namespace FusionComms.Migrations
{
    public partial class AddCustomerName : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CollectedName",
                table: "WhatsAppOrderSessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "WhatsAppCustomerProfiles",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CollectedName",
                table: "WhatsAppOrderSessions");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "WhatsAppCustomerProfiles");
        }
    }
}
