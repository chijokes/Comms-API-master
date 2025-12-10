using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FusionComms.Migrations
{
    public partial class WhatsAppTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CatalogId",
                table: "WhatsAppBusinesses",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WhatsAppProducts",
                columns: table => new
                {
                    ProductId = table.Column<string>(type: "text", nullable: false),
                    BusinessId = table.Column<string>(type: "text", nullable: true),
                    RetailerId = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    Currency = table.Column<string>(type: "text", nullable: true),
                    SalePrice = table.Column<decimal>(type: "numeric", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Condition = table.Column<string>(type: "text", nullable: true),
                    Availability = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: true),
                    Category = table.Column<string>(type: "text", nullable: true),
                    Subcategory = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhatsAppProducts", x => x.ProductId);
                });

            migrationBuilder.CreateTable(
                name: "WhatsAppProductSets",
                columns: table => new
                {
                    SetId = table.Column<string>(type: "text", nullable: false),
                    BusinessId = table.Column<string>(type: "text", nullable: true),
                    CatalogId = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: true),
                    FilterField = table.Column<string>(type: "text", nullable: true),
                    FilterValue = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhatsAppProductSets", x => x.SetId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WhatsAppProducts");

            migrationBuilder.DropTable(
                name: "WhatsAppProductSets");

            migrationBuilder.DropColumn(
                name: "CatalogId",
                table: "WhatsAppBusinesses");
        }
    }
}
