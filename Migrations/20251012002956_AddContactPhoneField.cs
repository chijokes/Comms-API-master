using Microsoft.EntityFrameworkCore.Migrations;

namespace FusionComms.Migrations
{
    public partial class AddContactPhoneField : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContactPhone",
                table: "WhatsAppCustomerProfiles",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContactPhone",
                table: "WhatsAppCustomerProfiles");
        }
    }
}
