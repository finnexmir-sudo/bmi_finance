using System;
using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <summary>
    /// Avtopark modulu — xidməti maşınlar (19.08.2026).
    ///
    /// BEŞ YENİ CƏDVƏL, mövcud dataya TOXUNMUR:
    ///   Masinlar                      — maşın kartı
    ///   MasinMuracietler              — müraciət + açar (çıxış/qayıdış) jurnalı
    ///   MasinMuddetNovleri            — sığorta/baxış/yağ… növləri (lookup)
    ///   MasinMuddetler                — müddət qeydləri
    ///   AvtoparkXeberdarliqAlicilari  — xəbərdarlığı kim alsın (admin seçir)
    ///
    /// BÜTÜN FK-lar `Restrict`-dir. `MasinMuracietler` cədvəlində İşçiyə dörd
    /// ayrı FK var (müraciətçi, rəhbər, çıxışı qeyd edən, qayıdışı qeyd edən) —
    /// Cascade qalsaydı SQL Server «multiple cascade paths» ilə cədvəli
    /// yaratmazdı. Layihədə silinmə onsuz da yumşaqdır (`Silinib`).
    ///
    /// SİNİF ADI QƏSDƏN «AvtoparkCedvelleri»-dir: `Avtopark` adı həm də
    /// namespace seqmentidir, eyni adlı sinif ad kölgələnməsi riski yaradar
    /// (CLAUDE.md — CS0118). Migration ID fayl adı ilə eyni qalır.
    ///
    /// Startup-da `db.Database.Migrate()` ilə avtomatik tətbiq olunur.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20260819000000_Avtopark")]
    public partial class AvtoparkCedvelleri : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Masinlar ────────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "Masinlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DovletNomresi = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Marka = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Model = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    BuraxilisIli = table.Column<int>(type: "int", nullable: true),
                    Reng = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Ban = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Vin = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Novu = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DepartamentId = table.Column<int>(type: "int", nullable: true),
                    TehkimSurucuId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CariKm = table.Column<int>(type: "int", nullable: true),
                    Qeyd = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_Masinlar", x => x.Id);
                    table.ForeignKey(
                        // Cədvəl adı «Departament»-dir (çoxluq DEYİL) — `AppDbContext`-də
                        // eyni entity üçün İKİ DbSet var (`Departments` + `Departamentler`),
                        // ona görə EF adı DbSet-dən yox, ENTITY adından götürür.
                        // Snapshot ilə təsdiqləndi: `b.ToTable("Departament")`.
                        name: "FK_Masinlar_Departament_DepartamentId",
                        column: x => x.DepartamentId,
                        principalTable: "Departament",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Masinlar_Isciler_TehkimSurucuId",
                        column: x => x.TehkimSurucuId,
                        principalTable: "Isciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // ── MasinMuracietler ────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "MasinMuracietler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MasinId = table.Column<int>(type: "int", nullable: false),
                    IsciId = table.Column<int>(type: "int", nullable: false),
                    PlanBaslama = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PlanBitme = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Meqsed = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Marsrut = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RehberId = table.Column<int>(type: "int", nullable: true),
                    RehberTesdiqTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ImtinaSebebi = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CixisTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CixisQeydEdenId = table.Column<int>(type: "int", nullable: true),
                    QayidisTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    QayidisQeydEdenId = table.Column<int>(type: "int", nullable: true),
                    CixisKm = table.Column<int>(type: "int", nullable: true),
                    QayidisKm = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_MasinMuracietler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MasinMuracietler_Masinlar_MasinId",
                        column: x => x.MasinId,
                        principalTable: "Masinlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MasinMuracietler_Isciler_IsciId",
                        column: x => x.IsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MasinMuracietler_Isciler_RehberId",
                        column: x => x.RehberId,
                        principalTable: "Isciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MasinMuracietler_Isciler_CixisQeydEdenId",
                        column: x => x.CixisQeydEdenId,
                        principalTable: "Isciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MasinMuracietler_Isciler_QayidisQeydEdenId",
                        column: x => x.QayidisQeydEdenId,
                        principalTable: "Isciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // ── MasinMuddetNovleri (lookup) ─────────────────────────────────
            migrationBuilder.CreateTable(
                name: "MasinMuddetNovleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ad = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    XeberdarliqGun = table.Column<int>(type: "int", nullable: false),
                    Aktivdir = table.Column<bool>(type: "bit", nullable: false),
                    Sira = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_MasinMuddetNovleri", x => x.Id);
                });

            // ── MasinMuddetler ──────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "MasinMuddetler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MasinId = table.Column<int>(type: "int", nullable: false),
                    NovId = table.Column<int>(type: "int", nullable: false),
                    SonTarix = table.Column<DateTime>(type: "datetime2", nullable: false),
                    XeberdarliqGun = table.Column<int>(type: "int", nullable: false),
                    Mebleg = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    SenedFaylYolu = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    SenedFaylAdi = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Qeyd = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Aktivdir = table.Column<bool>(type: "bit", nullable: false),
                    XeberdarliqGonderilib = table.Column<bool>(type: "bit", nullable: false),
                    XeberdarliqTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SonKm = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_MasinMuddetler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MasinMuddetler_Masinlar_MasinId",
                        column: x => x.MasinId,
                        principalTable: "Masinlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MasinMuddetler_MasinMuddetNovleri_NovId",
                        column: x => x.NovId,
                        principalTable: "MasinMuddetNovleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // ── AvtoparkXeberdarliqAlicilari ────────────────────────────────
            migrationBuilder.CreateTable(
                name: "AvtoparkXeberdarliqAlicilari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsciId = table.Column<int>(type: "int", nullable: false),
                    Aktivdir = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_AvtoparkXeberdarliqAlicilari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AvtoparkXeberdarliqAlicilari_Isciler_IsciId",
                        column: x => x.IsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // ── İndekslər ───────────────────────────────────────────────────
            migrationBuilder.CreateIndex(
                name: "IX_Masinlar_DovletNomresi", table: "Masinlar", column: "DovletNomresi");
            migrationBuilder.CreateIndex(
                name: "IX_Masinlar_DepartamentId", table: "Masinlar", column: "DepartamentId");
            migrationBuilder.CreateIndex(
                name: "IX_Masinlar_TehkimSurucuId", table: "Masinlar", column: "TehkimSurucuId");

            migrationBuilder.CreateIndex(
                name: "IX_MasinMuracietler_MasinId_Status_PlanBaslama",
                table: "MasinMuracietler",
                columns: new[] { "MasinId", "Status", "PlanBaslama" });
            migrationBuilder.CreateIndex(
                name: "IX_MasinMuracietler_IsciId_Status",
                table: "MasinMuracietler",
                columns: new[] { "IsciId", "Status" });
            migrationBuilder.CreateIndex(
                name: "IX_MasinMuracietler_RehberId", table: "MasinMuracietler", column: "RehberId");
            migrationBuilder.CreateIndex(
                name: "IX_MasinMuracietler_CixisQeydEdenId", table: "MasinMuracietler", column: "CixisQeydEdenId");
            migrationBuilder.CreateIndex(
                name: "IX_MasinMuracietler_QayidisQeydEdenId", table: "MasinMuracietler", column: "QayidisQeydEdenId");

            migrationBuilder.CreateIndex(
                name: "IX_MasinMuddetNovleri_Ad", table: "MasinMuddetNovleri", column: "Ad");

            migrationBuilder.CreateIndex(
                name: "IX_MasinMuddetler_Aktivdir_XeberdarliqGonderilib_SonTarix",
                table: "MasinMuddetler",
                columns: new[] { "Aktivdir", "XeberdarliqGonderilib", "SonTarix" });
            migrationBuilder.CreateIndex(
                name: "IX_MasinMuddetler_MasinId", table: "MasinMuddetler", column: "MasinId");
            migrationBuilder.CreateIndex(
                name: "IX_MasinMuddetler_NovId", table: "MasinMuddetler", column: "NovId");

            migrationBuilder.CreateIndex(
                name: "IX_AvtoparkXeberdarliqAlicilari_IsciId",
                table: "AvtoparkXeberdarliqAlicilari", column: "IsciId");

            // ── Standart müddət növləri ─────────────────────────────────────
            // Admin bunları dəyişə/silə/əlavə edə bilir — cədvəl boş qalmasın deyə
            // ilkin doldurulur. Yağ dəyişmə TARİXƏ görədir (ildə bir dəfə, 365 gün);
            // 19.08.2026 qərarı ilə kilometrə görə izləmə YOXDUR.
            migrationBuilder.InsertData(
                table: "MasinMuddetNovleri",
                columns: new[] { "Ad", "XeberdarliqGun", "Aktivdir", "Sira", "YaradilmaTarixi", "Silinib" },
                values: new object[,]
                {
                    { "İcbari sığorta",  30, true, 1, new DateTime(2026, 8, 19), false },
                    { "Kasko",           30, true, 2, new DateTime(2026, 8, 19), false },
                    { "Texniki baxış",   30, true, 3, new DateTime(2026, 8, 19), false },
                    { "Yağ dəyişmə",     14, true, 4, new DateTime(2026, 8, 19), false },
                    { "Yanğınsöndürən",  30, true, 5, new DateTime(2026, 8, 19), false }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AvtoparkXeberdarliqAlicilari");
            migrationBuilder.DropTable(name: "MasinMuddetler");
            migrationBuilder.DropTable(name: "MasinMuddetNovleri");
            migrationBuilder.DropTable(name: "MasinMuracietler");
            migrationBuilder.DropTable(name: "Masinlar");
        }
    }
}
