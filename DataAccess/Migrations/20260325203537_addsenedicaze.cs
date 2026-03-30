using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class addsenedicaze : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "senedDovriyyesiIstifadeciIcazeleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IstifadeciId = table.Column<int>(type: "int", nullable: false),
                    SobeId = table.Column<int>(type: "int", nullable: false),
                    DepartamentId = table.Column<int>(type: "int", nullable: true),
                    IcazeNovu = table.Column<int>(type: "int", nullable: false),
                    YaradilmaTarixi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    YaradanIcraciId = table.Column<int>(type: "int", nullable: true),
                    YenileyenIcraciId = table.Column<int>(type: "int", nullable: true),
                    SilenIcraciId = table.Column<int>(type: "int", nullable: true),
                    YenilenmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Silinib = table.Column<bool>(type: "bit", nullable: false),
                    SilinmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_senedDovriyyesiIstifadeciIcazeleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_senedDovriyyesiIstifadeciIcazeleri_AspNetUsers_IstifadeciId",
                        column: x => x.IstifadeciId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_senedDovriyyesiIstifadeciIcazeleri_Departament_DepartamentId",
                        column: x => x.DepartamentId,
                        principalTable: "Departament",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_senedDovriyyesiIstifadeciIcazeleri_DepartamentId",
                table: "senedDovriyyesiIstifadeciIcazeleri",
                column: "DepartamentId");

            migrationBuilder.CreateIndex(
                name: "IX_senedDovriyyesiIstifadeciIcazeleri_IstifadeciId",
                table: "senedDovriyyesiIstifadeciIcazeleri",
                column: "IstifadeciId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "senedDovriyyesiIstifadeciIcazeleri");
        }
    }
}
