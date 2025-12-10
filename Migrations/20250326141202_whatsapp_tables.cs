using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FusionComms.Migrations
{
    public partial class whatsapp_tables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WhatsAppBusinesses",
                columns: table => new
                {
                    BusinessId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ApplicationId = table.Column<string>(type: "text", nullable: false),
                    PhoneNumberId = table.Column<string>(type: "text", nullable: false),
                    AccountId = table.Column<string>(type: "text", nullable: false),
                    BusinessName = table.Column<string>(type: "text", nullable: false),
                    AccessToken = table.Column<string>(type: "text", nullable: false),
                    VerifyToken = table.Column<string>(type: "text", nullable: false),
                    AppSecret = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhatsAppBusinesses", x => x.BusinessId);
                });

            migrationBuilder.CreateTable(
                name: "WhatsAppMedia",
                columns: table => new
                {
                    MediaId = table.Column<string>(type: "text", nullable: false),
                    BusinessId = table.Column<string>(type: "character varying(50)", nullable: true),
                    FileName = table.Column<string>(type: "text", nullable: true),
                    FileType = table.Column<string>(type: "text", nullable: true),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    Caption = table.Column<string>(type: "text", nullable: true),
                    UploadTimestamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ExpirationDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhatsAppMedia", x => x.MediaId);
                    table.ForeignKey(
                        name: "FK_WhatsAppMedia_WhatsAppBusinesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "WhatsAppBusinesses",
                        principalColumn: "BusinessId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WhatsAppTemplates",
                columns: table => new
                {
                    TemplateId = table.Column<string>(type: "text", nullable: false),
                    TemplateName = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    BusinessId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ParameterCount = table.Column<int>(type: "integer", nullable: false),
                    BodyText = table.Column<string>(type: "text", nullable: true),
                    ExampleBodyText = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhatsAppTemplates", x => x.TemplateId);
                    table.ForeignKey(
                        name: "FK_WhatsAppTemplates_WhatsAppBusinesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "WhatsAppBusinesses",
                        principalColumn: "BusinessId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WhatsAppMessages",
                columns: table => new
                {
                    MessageId = table.Column<string>(type: "text", nullable: false),
                    ProviderMessageId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    BusinessId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TemplateId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    MediaId = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: true),
                    Direction = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ErrorCode = table.Column<string>(type: "text", nullable: true),
                    ErrorTitle = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhatsAppMessages", x => x.MessageId);
                    table.ForeignKey(
                        name: "FK_WhatsAppMessages_WhatsAppBusinesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "WhatsAppBusinesses",
                        principalColumn: "BusinessId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WhatsAppMessages_WhatsAppMedia_MediaId",
                        column: x => x.MediaId,
                        principalTable: "WhatsAppMedia",
                        principalColumn: "MediaId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WhatsAppMessages_WhatsAppTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "WhatsAppTemplates",
                        principalColumn: "TemplateId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppMedia_BusinessId",
                table: "WhatsAppMedia",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppMessages_BusinessId",
                table: "WhatsAppMessages",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppMessages_MediaId",
                table: "WhatsAppMessages",
                column: "MediaId");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppMessages_TemplateId",
                table: "WhatsAppMessages",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppTemplates_BusinessId",
                table: "WhatsAppTemplates",
                column: "BusinessId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WhatsAppMessages");

            migrationBuilder.DropTable(
                name: "WhatsAppMedia");

            migrationBuilder.DropTable(
                name: "WhatsAppTemplates");

            migrationBuilder.DropTable(
                name: "WhatsAppBusinesses");
        }
    }
}
