using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class Isci_IcraciNo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IcraciNo",
                table: "Isciler",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Isciler_IcraciNo",
                table: "Isciler",
                column: "IcraciNo",
                unique: true,
                filter: "[IcraciNo] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Isciler_IcraciNo",
                table: "Isciler");

            migrationBuilder.DropColumn(
                name: "IcraciNo",
                table: "Isciler");
        }
    }
}
