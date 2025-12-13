using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PsP.Migrations
{
    public partial class AddBusinessTypeToBusinesses : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BusinessType",
                table: "Businesses",
                type: "text",
                nullable: false,
                defaultValue: ""
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BusinessType",
                table: "Businesses"
            );
        }
    }
}