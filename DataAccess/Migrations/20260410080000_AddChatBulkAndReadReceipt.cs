using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddChatBulkAndReadReceipt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "OxunmaTarixi",
                table: "ChatMesajlar",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TopluMesajGrupId",
                table: "ChatMesajlar",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChatMesajlar_TopluMesajGrupId",
                table: "ChatMesajlar",
                column: "TopluMesajGrupId",
                filter: "[TopluMesajGrupId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ChatMesajlar_TopluMesajGrupId",
                table: "ChatMesajlar");

            migrationBuilder.DropColumn(
                name: "TopluMesajGrupId",
                table: "ChatMesajlar");

            migrationBuilder.DropColumn(
                name: "OxunmaTarixi",
                table: "ChatMesajlar");
        }
    }
}
