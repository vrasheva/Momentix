using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Momentix.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLetterUnlockDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "UnlockAt",
                table: "MediaItems",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UnlockAt",
                table: "MediaItems");
        }
    }
}
