using Microsoft.EntityFrameworkCore.Migrations;

namespace FusionComms.Migrations
{
    public partial class RenameWhatsAppTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_Orders_OrderId",
                table: "OrderItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_WhatsAppBusinesses_BusinessId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderSessions_WhatsAppBusinesses_BusinessId",
                table: "OrderSessions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OrderSessions",
                table: "OrderSessions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Orders",
                table: "Orders");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OrderItems",
                table: "OrderItems");

            migrationBuilder.RenameTable(
                name: "OrderSessions",
                newName: "WhatsAppOrderSessions");

            migrationBuilder.RenameTable(
                name: "Orders",
                newName: "WhatsAppOrders");

            migrationBuilder.RenameTable(
                name: "OrderItems",
                newName: "WhatsAppOrderItems");

            migrationBuilder.RenameIndex(
                name: "IX_OrderSessions_BusinessId",
                table: "WhatsAppOrderSessions",
                newName: "IX_WhatsAppOrderSessions_BusinessId");

            migrationBuilder.RenameIndex(
                name: "IX_Orders_BusinessId",
                table: "WhatsAppOrders",
                newName: "IX_WhatsAppOrders_BusinessId");

            migrationBuilder.RenameIndex(
                name: "IX_OrderItems_OrderId",
                table: "WhatsAppOrderItems",
                newName: "IX_WhatsAppOrderItems_OrderId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_WhatsAppOrderSessions",
                table: "WhatsAppOrderSessions",
                column: "SessionId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_WhatsAppOrders",
                table: "WhatsAppOrders",
                column: "OrderId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_WhatsAppOrderItems",
                table: "WhatsAppOrderItems",
                column: "ItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_WhatsAppOrderItems_WhatsAppOrders_OrderId",
                table: "WhatsAppOrderItems",
                column: "OrderId",
                principalTable: "WhatsAppOrders",
                principalColumn: "OrderId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WhatsAppOrders_WhatsAppBusinesses_BusinessId",
                table: "WhatsAppOrders",
                column: "BusinessId",
                principalTable: "WhatsAppBusinesses",
                principalColumn: "BusinessId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WhatsAppOrderSessions_WhatsAppBusinesses_BusinessId",
                table: "WhatsAppOrderSessions",
                column: "BusinessId",
                principalTable: "WhatsAppBusinesses",
                principalColumn: "BusinessId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WhatsAppOrderItems_WhatsAppOrders_OrderId",
                table: "WhatsAppOrderItems");

            migrationBuilder.DropForeignKey(
                name: "FK_WhatsAppOrders_WhatsAppBusinesses_BusinessId",
                table: "WhatsAppOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_WhatsAppOrderSessions_WhatsAppBusinesses_BusinessId",
                table: "WhatsAppOrderSessions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_WhatsAppOrderSessions",
                table: "WhatsAppOrderSessions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_WhatsAppOrders",
                table: "WhatsAppOrders");

            migrationBuilder.DropPrimaryKey(
                name: "PK_WhatsAppOrderItems",
                table: "WhatsAppOrderItems");

            migrationBuilder.RenameTable(
                name: "WhatsAppOrderSessions",
                newName: "OrderSessions");

            migrationBuilder.RenameTable(
                name: "WhatsAppOrders",
                newName: "Orders");

            migrationBuilder.RenameTable(
                name: "WhatsAppOrderItems",
                newName: "OrderItems");

            migrationBuilder.RenameIndex(
                name: "IX_WhatsAppOrderSessions_BusinessId",
                table: "OrderSessions",
                newName: "IX_OrderSessions_BusinessId");

            migrationBuilder.RenameIndex(
                name: "IX_WhatsAppOrders_BusinessId",
                table: "Orders",
                newName: "IX_Orders_BusinessId");

            migrationBuilder.RenameIndex(
                name: "IX_WhatsAppOrderItems_OrderId",
                table: "OrderItems",
                newName: "IX_OrderItems_OrderId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_OrderSessions",
                table: "OrderSessions",
                column: "SessionId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Orders",
                table: "Orders",
                column: "OrderId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_OrderItems",
                table: "OrderItems",
                column: "ItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_Orders_OrderId",
                table: "OrderItems",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "OrderId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_WhatsAppBusinesses_BusinessId",
                table: "Orders",
                column: "BusinessId",
                principalTable: "WhatsAppBusinesses",
                principalColumn: "BusinessId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderSessions_WhatsAppBusinesses_BusinessId",
                table: "OrderSessions",
                column: "BusinessId",
                principalTable: "WhatsAppBusinesses",
                principalColumn: "BusinessId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
