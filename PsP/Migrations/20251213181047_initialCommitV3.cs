using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PsP.Migrations
{
    /// <inheritdoc />
    public partial class initialCommitV3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AppointmentEnd",
                table: "Reservations");

            migrationBuilder.AddColumn<int>(
                name: "OrderId",
                table: "Reservations",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrderId",
                table: "Reservations");

            migrationBuilder.AddColumn<DateTime>(
                name: "AppointmentEnd",
                table: "Reservations",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
