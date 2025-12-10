using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FusionComms.Migrations
{
    public partial class AddNewerWhatsAppBotTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerEmails");

            migrationBuilder.DropPrimaryKey(
                name: "PK_WhatsAppProducts",
                table: "WhatsAppProducts");

            migrationBuilder.DropColumn(
                name: "AccessToken",
                table: "WhatsAppBusinesses");

            migrationBuilder.DropColumn(
                name: "AccessToken2",
                table: "WhatsAppBusinesses");

            migrationBuilder.DropColumn(
                name: "AppSecret",
                table: "WhatsAppBusinesses");

            migrationBuilder.RenameColumn(
                name: "VerifyToken",
                table: "WhatsAppBusinesses",
                newName: "BusinessToken");

            migrationBuilder.RenameColumn(
                name: "ApplicationId",
                table: "WhatsAppBusinesses",
                newName: "AppId");

            migrationBuilder.AddColumn<string>(
                name: "BotName",
                table: "WhatsAppBusinesses",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

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

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "OrderSessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfileState",
                table: "OrderSessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_WhatsAppProducts",
                table: "WhatsAppProducts",
                columns: new[] { "ProductId", "RevenueCenterId" });

            migrationBuilder.CreateTable(
                name: "WhatsAppAppConfigs",
                columns: table => new
                {
                    ConfigId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AppId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AppSecret = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    AccessToken = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    VerifyToken = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhatsAppAppConfigs", x => x.ConfigId);
                });

            migrationBuilder.CreateTable(
                name: "WhatsAppCustomerProfiles",
                columns: table => new
                {
                    ProfileId = table.Column<string>(type: "text", nullable: false),
                    BusinessId = table.Column<string>(type: "character varying(50)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhatsAppCustomerProfiles", x => x.ProfileId);
                    table.ForeignKey(
                        name: "FK_WhatsAppCustomerProfiles_WhatsAppBusinesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "WhatsAppBusinesses",
                        principalColumn: "BusinessId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WhatsAppDeliveryAddresses",
                columns: table => new
                {
                    AddressId = table.Column<string>(type: "text", nullable: false),
                    ProfileId = table.Column<string>(type: "text", nullable: true),
                    Address = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhatsAppDeliveryAddresses", x => x.AddressId);
                    table.ForeignKey(
                        name: "FK_WhatsAppDeliveryAddresses_WhatsAppCustomerProfiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "WhatsAppCustomerProfiles",
                        principalColumn: "ProfileId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppCustomerProfiles_BusinessId",
                table: "WhatsAppCustomerProfiles",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppDeliveryAddresses_ProfileId",
                table: "WhatsAppDeliveryAddresses",
                column: "ProfileId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WhatsAppAppConfigs");

            migrationBuilder.DropTable(
                name: "WhatsAppDeliveryAddresses");

            migrationBuilder.DropTable(
                name: "WhatsAppCustomerProfiles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_WhatsAppProducts",
                table: "WhatsAppProducts");

            migrationBuilder.DropColumn(
                name: "BotName",
                table: "WhatsAppBusinesses");

            migrationBuilder.DropColumn(
                name: "HelpEmail",
                table: "WhatsAppBusinesses");

            migrationBuilder.DropColumn(
                name: "HelpPhone",
                table: "WhatsAppBusinesses");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "OrderSessions");

            migrationBuilder.DropColumn(
                name: "ProfileState",
                table: "OrderSessions");

            migrationBuilder.RenameColumn(
                name: "BusinessToken",
                table: "WhatsAppBusinesses",
                newName: "VerifyToken");

            migrationBuilder.RenameColumn(
                name: "AppId",
                table: "WhatsAppBusinesses",
                newName: "ApplicationId");

            migrationBuilder.AddColumn<string>(
                name: "AccessToken",
                table: "WhatsAppBusinesses",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AccessToken2",
                table: "WhatsAppBusinesses",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AppSecret",
                table: "WhatsAppBusinesses",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_WhatsAppProducts",
                table: "WhatsAppProducts",
                column: "ProductId");

            migrationBuilder.CreateTable(
                name: "CustomerEmails",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    BusinessId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CustomerName = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: false),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    PhoneNumber = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerEmails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerEmails_WhatsAppBusinesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "WhatsAppBusinesses",
                        principalColumn: "BusinessId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerEmails_BusinessId",
                table: "CustomerEmails",
                column: "BusinessId");
        }
    }
}
