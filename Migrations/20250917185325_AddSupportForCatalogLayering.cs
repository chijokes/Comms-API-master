using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FusionComms.Migrations
{
    public partial class AddSupportForCatalogLayering : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CurrentCategoryGroup",
                table: "WhatsAppOrderSessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrentMenuLevel",
                table: "WhatsAppOrderSessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrentSubcategoryGroup",
                table: "WhatsAppOrderSessions",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WhatsAppProductSetGroupings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    BusinessId = table.Column<string>(type: "text", nullable: false),
                    GroupName = table.Column<string>(type: "text", nullable: false),
                    ProductSetIds = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhatsAppProductSetGroupings", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WhatsAppProductSetGroupings");

            migrationBuilder.DropColumn(
                name: "CurrentCategoryGroup",
                table: "WhatsAppOrderSessions");

            migrationBuilder.DropColumn(
                name: "CurrentMenuLevel",
                table: "WhatsAppOrderSessions");

            migrationBuilder.DropColumn(
                name: "CurrentSubcategoryGroup",
                table: "WhatsAppOrderSessions");
        }
    }
}
