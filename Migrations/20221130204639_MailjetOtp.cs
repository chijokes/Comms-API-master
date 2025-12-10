using Microsoft.EntityFrameworkCore.Migrations;

namespace FusionComms.Migrations
{
    public partial class MailjetOtp : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MailJetOtpId",
                table: "RegisteredSesUsers",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MailJetOtpId",
                table: "RegisteredSesUsers");
        }
    }
}
