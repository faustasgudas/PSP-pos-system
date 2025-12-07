using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PsP.Migrations
{
    /// <inheritdoc />
    public partial class AddNoteToStockMovement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "StockMovements",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "StockItems",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Note",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "StockItems");
        }
    }
}
