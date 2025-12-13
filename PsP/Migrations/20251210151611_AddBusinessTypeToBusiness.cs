using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PsP.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessTypeToBusiness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "BusinessStatus",
                table: "Businesses",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Active",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "BusinessType",
                table: "Businesses",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Catering");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BusinessType",
                table: "Businesses");

            migrationBuilder.AlterColumn<string>(
                name: "BusinessStatus",
                table: "Businesses",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldDefaultValue: "Active");
        }
    }
}
