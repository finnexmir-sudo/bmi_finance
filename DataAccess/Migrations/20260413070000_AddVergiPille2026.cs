using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// 2026 pilləli vergi dərəcələri üçün VergiPilleleri cədvəli və seed data.
    /// Həmçinin "İTSS (İşəgötürən)" MaasNovu seed əlavə edilir.
    /// </summary>
    public partial class AddVergiPille2026 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── VergiPilleleri cədvəli ──────────────────────────────
            migrationBuilder.CreateTable(
                name: "VergiPilleleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nov = table.Column<int>(type: "int", nullable: false),
                    Sira = table.Column<int>(type: "int", nullable: false),
                    AsagiHedd = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    YuxariHedd = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Faiz = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    SabitMebleg = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BaslamaTarixi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BitmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Aktivdir = table.Column<bool>(type: "bit", nullable: false),
                    Aciqlama = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
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
                    table.PrimaryKey("PK_VergiPilleleri", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VergiPilleleri_Nov_Aktivdir",
                table: "VergiPilleleri",
                columns: new[] { "Nov", "Aktivdir" });

            // ── Yeni MaasNovu: İTSS (İşəgötürən) ────────────────────
            migrationBuilder.InsertData(
                table: "MaasNovleri",
                columns: new[] { "Id", "Ad", "Aktivdir", "SilenIcraciId", "Silinib", "SilinmeTarixi", "Tip", "YaradanIcraciId", "YaradilmaTarixi", "YenilenmeTarixi", "YenileyenIcraciId" },
                values: new object[] { 12, "İTSS (İşəgötürən)", true, null, false, null, 3 /* IsegoturenXerci */, null, new DateTime(2026, 1, 1), null, null });

            // ── VergiPille seed data (2026 rəsmi dərəcələri) ────────
            var createdAt = new DateTime(2026, 1, 1);
            var baslama = new DateTime(2026, 1, 1);

            migrationBuilder.InsertData(
                table: "VergiPilleleri",
                columns: new[] { "Id", "Nov", "Sira", "AsagiHedd", "YuxariHedd", "Faiz", "SabitMebleg", "BaslamaTarixi", "BitmeTarixi", "Aktivdir", "Aciqlama", "SilenIcraciId", "Silinib", "SilinmeTarixi", "YaradanIcraciId", "YaradilmaTarixi", "YenilenmeTarixi", "YenileyenIcraciId" },
                values: new object[,]
                {
                    // Gəlir Vergisi
                    { 1, 1, 1, 0m,    (decimal?)2500m, 3m,  0m,    baslama, (DateTime?)null, true, "2026: 0–2500 AZN → 3%",          (int?)null, false, (DateTime?)null, (int?)null, createdAt, (DateTime?)null, (int?)null },
                    { 2, 1, 2, 2500m, (decimal?)8000m, 10m, 75m,   baslama, (DateTime?)null, true, "2026: 2500–8000 AZN → 75+10%",   (int?)null, false, (DateTime?)null, (int?)null, createdAt, (DateTime?)null, (int?)null },
                    { 3, 1, 3, 8000m, (decimal?)null,  14m, 625m,  baslama, (DateTime?)null, true, "2026: 8000+ AZN → 625+14%",      (int?)null, false, (DateTime?)null, (int?)null, createdAt, (DateTime?)null, (int?)null },

                    // DSMF (İşçi)
                    { 4, 2, 1, 0m,   (decimal?)200m, 3m,  0m, baslama, (DateTime?)null, true, "2026: 0–200 AZN → 3%",      (int?)null, false, (DateTime?)null, (int?)null, createdAt, (DateTime?)null, (int?)null },
                    { 5, 2, 2, 200m, (decimal?)null, 10m, 6m, baslama, (DateTime?)null, true, "2026: 200+ AZN → 6+10%",    (int?)null, false, (DateTime?)null, (int?)null, createdAt, (DateTime?)null, (int?)null },

                    // DSMF (İşəgötürən)
                    { 6, 7, 1, 0m,    (decimal?)200m,  22m, 0m,    baslama, (DateTime?)null, true, "2026: 0–200 AZN → 22%",         (int?)null, false, (DateTime?)null, (int?)null, createdAt, (DateTime?)null, (int?)null },
                    { 7, 7, 2, 200m,  (decimal?)8000m, 15m, 44m,   baslama, (DateTime?)null, true, "2026: 200–8000 AZN → 44+15%",   (int?)null, false, (DateTime?)null, (int?)null, createdAt, (DateTime?)null, (int?)null },
                    { 8, 7, 3, 8000m, (decimal?)null,  11m, 1214m, baslama, (DateTime?)null, true, "2026: 8000+ AZN → 1214+11%",    (int?)null, false, (DateTime?)null, (int?)null, createdAt, (DateTime?)null, (int?)null },

                    // İTSS (İşçi)
                    { 9,  4, 1, 0m,    (decimal?)8000m, 2m,   0m,   baslama, (DateTime?)null, true, "2026: 0–8000 AZN → 2%",        (int?)null, false, (DateTime?)null, (int?)null, createdAt, (DateTime?)null, (int?)null },
                    { 10, 4, 2, 8000m, (decimal?)null,  0.5m, 160m, baslama, (DateTime?)null, true, "2026: 8000+ AZN → 160+0.5%",   (int?)null, false, (DateTime?)null, (int?)null, createdAt, (DateTime?)null, (int?)null },

                    // İTSS (İşəgötürən)
                    { 11, 9, 1, 0m,    (decimal?)8000m, 2m,   0m,   baslama, (DateTime?)null, true, "2026: 0–8000 AZN → 2%",        (int?)null, false, (DateTime?)null, (int?)null, createdAt, (DateTime?)null, (int?)null },
                    { 12, 9, 2, 8000m, (decimal?)null,  0.5m, 160m, baslama, (DateTime?)null, true, "2026: 8000+ AZN → 160+0.5%",   (int?)null, false, (DateTime?)null, (int?)null, createdAt, (DateTime?)null, (int?)null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VergiPilleleri");

            migrationBuilder.DeleteData(
                table: "MaasNovleri",
                keyColumn: "Id",
                keyValue: 12);
        }
    }
}
