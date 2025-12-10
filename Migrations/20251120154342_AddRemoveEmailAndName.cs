using Microsoft.EntityFrameworkCore.Migrations;

namespace FusionComms.Migrations
{
    public partial class AddRemoveEmailAndName : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                table: "WhatsAppCustomerProfiles");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "WhatsAppCustomerProfiles");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "WhatsAppCustomerProfiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "WhatsAppCustomerProfiles",
                type: "text",
                nullable: true);
        }
    }
}
