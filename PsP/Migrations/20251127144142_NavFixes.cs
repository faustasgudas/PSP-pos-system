using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PsP.Migrations
{
    /// <inheritdoc />
    public partial class NavFixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderLines_CatalogItems_CatalogItemId1",
                table: "OrderLines");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderLines_Orders_OrderId1",
                table: "OrderLines");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Businesses_BusinessId1",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Orders_OrderId1",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_OrderLines_OrderLineId1",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_OrderLineId1",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_Payments_BusinessId1",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_OrderId1",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_OrderLines_CatalogItemId1",
                table: "OrderLines");

            migrationBuilder.DropIndex(
                name: "IX_OrderLines_OrderId1",
                table: "OrderLines");

            migrationBuilder.DropColumn(
                name: "OrderLineId1",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "BusinessId1",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "OrderId1",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "CatalogItemId1",
                table: "OrderLines");

            migrationBuilder.DropColumn(
                name: "OrderId1",
                table: "OrderLines");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OrderLineId1",
                table: "StockMovements",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BusinessId1",
                table: "Payments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OrderId1",
                table: "Payments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CatalogItemId1",
                table: "OrderLines",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OrderId1",
                table: "OrderLines",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_OrderLineId1",
                table: "StockMovements",
                column: "OrderLineId1");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_BusinessId1",
                table: "Payments",
                column: "BusinessId1");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_OrderId1",
                table: "Payments",
                column: "OrderId1");

            migrationBuilder.CreateIndex(
                name: "IX_OrderLines_CatalogItemId1",
                table: "OrderLines",
                column: "CatalogItemId1");

            migrationBuilder.CreateIndex(
                name: "IX_OrderLines_OrderId1",
                table: "OrderLines",
                column: "OrderId1");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderLines_CatalogItems_CatalogItemId1",
                table: "OrderLines",
                column: "CatalogItemId1",
                principalTable: "CatalogItems",
                principalColumn: "CatalogItemId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderLines_Orders_OrderId1",
                table: "OrderLines",
                column: "OrderId1",
                principalTable: "Orders",
                principalColumn: "OrderId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Businesses_BusinessId1",
                table: "Payments",
                column: "BusinessId1",
                principalTable: "Businesses",
                principalColumn: "BusinessId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Orders_OrderId1",
                table: "Payments",
                column: "OrderId1",
                principalTable: "Orders",
                principalColumn: "OrderId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_OrderLines_OrderLineId1",
                table: "StockMovements",
                column: "OrderLineId1",
                principalTable: "OrderLines",
                principalColumn: "OrderLineId");
        }
    }
}
