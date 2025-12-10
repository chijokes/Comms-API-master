using Microsoft.EntityFrameworkCore.Migrations;

namespace FusionComms.Migrations
{
    public partial class added_primary_provider_to_user : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PrimaryProvider",
                table: "RegisteredSesUsers",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrimaryProvider",
                table: "RegisteredSesUsers");
        }
    }
}
