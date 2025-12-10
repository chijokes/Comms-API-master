using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FusionComms.Migrations
{
    public partial class WhatsAppTablesNew : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FilterField",
                table: "WhatsAppProductSets");

            migrationBuilder.DropColumn(
                name: "FilterValue",
                table: "WhatsAppProductSets");

            migrationBuilder.DropColumn(
                name: "Availability",
                table: "WhatsAppProducts");

            migrationBuilder.DropColumn(
                name: "BusinessId",
                table: "WhatsAppProducts");

            migrationBuilder.DropColumn(
                name: "Condition",
                table: "WhatsAppProducts");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "WhatsAppProducts");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "WhatsAppProducts");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "WhatsAppProducts");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "WhatsAppProducts");

            migrationBuilder.DropColumn(
                name: "SalePrice",
                table: "WhatsAppProducts");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "WhatsAppProducts");

            // migrationBuilder.DropColumn(
            //     name: "PrimaryProvider",
            //     table: "RegisteredSesUsers");

            migrationBuilder.RenameColumn(
                name: "Subcategory",
                table: "WhatsAppProducts",
                newName: "SetId");

            migrationBuilder.AlterColumn<string>(
                name: "BusinessId",
                table: "WhatsAppProductSets",
                type: "character varying(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RetailerId",
                table: "WhatsAppProducts",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "WhatsAppProducts",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "WhatsAppProducts",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AccessToken2",
                table: "WhatsAppBusinesses",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CustomChannelId",
                table: "WhatsAppBusinesses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RestaurantId",
                table: "WhatsAppBusinesses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RevenueCenterId",
                table: "WhatsAppBusinesses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceId",
                table: "WhatsAppBusinesses",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    OrderId = table.Column<string>(type: "text", nullable: false),
                    BusinessId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PhoneNumber = table.Column<string>(type: "text", nullable: false),
                    OrderDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric", nullable: false),
                    Tax = table.Column<decimal>(type: "numeric", nullable: false),
                    Total = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.OrderId);
                    table.ForeignKey(
                        name: "FK_Orders_WhatsAppBusinesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "WhatsAppBusinesses",
                        principalColumn: "BusinessId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderSessions",
                columns: table => new
                {
                    SessionId = table.Column<string>(type: "text", nullable: false),
                    BusinessId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PhoneNumber = table.Column<string>(type: "text", nullable: false),
                    CurrentState = table.Column<string>(type: "text", nullable: false),
                    CartData = table.Column<string>(type: "text", nullable: true),
                    PendingParents = table.Column<string>(type: "text", nullable: true),
                    LastInteraction = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsEditing = table.Column<bool>(type: "boolean", nullable: false),
                    DeliveryMethod = table.Column<string>(type: "text", nullable: true),
                    DeliveryAddress = table.Column<string>(type: "text", nullable: true),
                    EditGroupsData = table.Column<string>(type: "text", nullable: true),
                    EditingGroupId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderSessions", x => x.SessionId);
                    table.ForeignKey(
                        name: "FK_OrderSessions_WhatsAppBusinesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "WhatsAppBusinesses",
                        principalColumn: "BusinessId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderItems",
                columns: table => new
                {
                    ItemId = table.Column<string>(type: "text", nullable: false),
                    OrderId = table.Column<string>(type: "text", nullable: false),
                    ProductId = table.Column<string>(type: "text", nullable: true),
                    ItemName = table.Column<string>(type: "text", nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    ComboPartnerId = table.Column<string>(type: "text", nullable: true),
                    Toppings = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItems", x => x.ItemId);
                    table.ForeignKey(
                        name: "FK_OrderItems_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppProductSets_BusinessId",
                table: "WhatsAppProductSets",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppProducts_SetId",
                table: "WhatsAppProducts",
                column: "SetId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderId",
                table: "OrderItems",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_BusinessId",
                table: "Orders",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderSessions_BusinessId",
                table: "OrderSessions",
                column: "BusinessId");

            migrationBuilder.AddForeignKey(
                name: "FK_WhatsAppProducts_WhatsAppProductSets_SetId",
                table: "WhatsAppProducts",
                column: "SetId",
                principalTable: "WhatsAppProductSets",
                principalColumn: "SetId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WhatsAppProductSets_WhatsAppBusinesses_BusinessId",
                table: "WhatsAppProductSets",
                column: "BusinessId",
                principalTable: "WhatsAppBusinesses",
                principalColumn: "BusinessId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WhatsAppProducts_WhatsAppProductSets_SetId",
                table: "WhatsAppProducts");

            migrationBuilder.DropForeignKey(
                name: "FK_WhatsAppProductSets_WhatsAppBusinesses_BusinessId",
                table: "WhatsAppProductSets");

            migrationBuilder.DropTable(
                name: "OrderItems");

            migrationBuilder.DropTable(
                name: "OrderSessions");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_WhatsAppProductSets_BusinessId",
                table: "WhatsAppProductSets");

            migrationBuilder.DropIndex(
                name: "IX_WhatsAppProducts_SetId",
                table: "WhatsAppProducts");

            migrationBuilder.DropColumn(
                name: "AccessToken2",
                table: "WhatsAppBusinesses");

            migrationBuilder.DropColumn(
                name: "CustomChannelId",
                table: "WhatsAppBusinesses");

            migrationBuilder.DropColumn(
                name: "RestaurantId",
                table: "WhatsAppBusinesses");

            migrationBuilder.DropColumn(
                name: "RevenueCenterId",
                table: "WhatsAppBusinesses");

            migrationBuilder.DropColumn(
                name: "SourceId",
                table: "WhatsAppBusinesses");

            migrationBuilder.RenameColumn(
                name: "SetId",
                table: "WhatsAppProducts",
                newName: "Subcategory");

            migrationBuilder.AlterColumn<string>(
                name: "BusinessId",
                table: "WhatsAppProductSets",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FilterField",
                table: "WhatsAppProductSets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FilterValue",
                table: "WhatsAppProductSets",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RetailerId",
                table: "WhatsAppProducts",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "WhatsAppProducts",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "WhatsAppProducts",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "Availability",
                table: "WhatsAppProducts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BusinessId",
                table: "WhatsAppProducts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Condition",
                table: "WhatsAppProducts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "WhatsAppProducts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "WhatsAppProducts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "WhatsAppProducts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "WhatsAppProducts",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SalePrice",
                table: "WhatsAppProducts",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "WhatsAppProducts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrimaryProvider",
                table: "RegisteredSesUsers",
                type: "text",
                nullable: true);
        }
    }
}
