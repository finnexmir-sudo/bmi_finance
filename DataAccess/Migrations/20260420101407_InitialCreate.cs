using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FinNex.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ad = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Soyad = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QeydiyyatTarixi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Aktivdir = table.Column<bool>(type: "bit", nullable: false),
                    IsciId = table.Column<int>(type: "int", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Banklar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ad = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Kod = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Voen = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SwiftBic = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MuxHesab = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_Banklar", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BayramGunleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ad = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tarix = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HerIlTeyinOlunur = table.Column<bool>(type: "bit", nullable: false),
                    Tip = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_BayramGunleri", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Departament",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ad = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Aciqlama = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_Departament", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Guzestler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ad = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Mebleg = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Madde = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
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
                    table.PrimaryKey("PK_Guzestler", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HesabatKateqoriyalari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ad = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_HesabatKateqoriyalari", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "XercKateqoriyalari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ad = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Ikon = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_XercKateqoriyalari", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LoginLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    FailReason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    LoginTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MaasNovleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ad = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Tip = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_MaasNovleri", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MaasParametrleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nov = table.Column<int>(type: "int", nullable: false),
                    Tip = table.Column<int>(type: "int", nullable: false),
                    Deyer = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Aciqlama = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    BaslamaTarixi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BitmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_MaasParametrleri", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Musteriler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ad = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Voen = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_Musteriler", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OdenisTapsirigiNomreleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Il = table.Column<int>(type: "int", nullable: false),
                    SonNomre = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_OdenisTapsirigiNomreleri", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Kod = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Ad = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Aciqlama = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tagler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ad = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_Tagler", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Telimler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ad = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tesvir = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tesviqci = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Mekan = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BaslamaTarixi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BitmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MuddetSaat = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DaxiliTelimdir = table.Column<bool>(type: "bit", nullable: false),
                    Xerc = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
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
                    table.PrimaryKey("PK_Telimler", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Valyutalar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Kod = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Ad = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_Valyutalar", x => x.Id);
                });

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

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Isciler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ad = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Soyad = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AtaAdi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FIN = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SeriyaNomre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DogumTarixi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Cins = table.Column<int>(type: "int", nullable: false),
                    Telefon = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Unvan = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsheQebulTarixi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsdenAyrilmaTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AppUserId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_Isciler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Isciler_AspNetUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Budceler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DepartamentId = table.Column<int>(type: "int", nullable: false),
                    Il = table.Column<int>(type: "int", nullable: false),
                    Ay = table.Column<int>(type: "int", nullable: false),
                    PlanMebleg = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FaktikiMebleg = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Qeyd = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_Budceler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Budceler_Departament_DepartamentId",
                        column: x => x.DepartamentId,
                        principalTable: "Departament",
                        principalColumn: "Id");
                });

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

            migrationBuilder.CreateTable(
                name: "SenedNovleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    Kod = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Ad = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Aktiv = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_SenedNovleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SenedNovleri_Departament_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departament",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserDepartments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    Esasdir = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
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
                    table.PrimaryKey("PK_UserDepartments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserDepartments_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserDepartments_Departament_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departament",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Vezifeler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ad = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Tesvir = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Aktivdir = table.Column<bool>(type: "bit", nullable: false),
                    DepartamentId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_Vezifeler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vezifeler_Departament_DepartamentId",
                        column: x => x.DepartamentId,
                        principalTable: "Departament",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserPermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    PermissionId = table.Column<int>(type: "int", nullable: false),
                    Allowed = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_UserPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPermissions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserPermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BankHesablari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BankId = table.Column<int>(type: "int", nullable: false),
                    Iban = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ValyutaId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_BankHesablari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BankHesablari_Banklar_BankId",
                        column: x => x.BankId,
                        principalTable: "Banklar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BankHesablari_Valyutalar_ValyutaId",
                        column: x => x.ValyutaId,
                        principalTable: "Valyutalar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MusteriHesablari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MusteriId = table.Column<int>(type: "int", nullable: false),
                    Iban = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ValyutaId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_MusteriHesablari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MusteriHesablari_Musteriler_MusteriId",
                        column: x => x.MusteriId,
                        principalTable: "Musteriler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MusteriHesablari_Valyutalar_ValyutaId",
                        column: x => x.ValyutaId,
                        principalTable: "Valyutalar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Avanslar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsciId = table.Column<int>(type: "int", nullable: false),
                    Il = table.Column<int>(type: "int", nullable: false),
                    Ay = table.Column<int>(type: "int", nullable: false),
                    Mebleg = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Sebeb = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    MuhasibId = table.Column<int>(type: "int", nullable: true),
                    TesdiqTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ImtinaSebebi = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_Avanslar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Avanslar_Isciler_IsciId",
                        column: x => x.IsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Avanslar_Isciler_MuhasibId",
                        column: x => x.MuhasibId,
                        principalTable: "Isciler",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Bildirisler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsciId = table.Column<int>(type: "int", nullable: false),
                    Nov = table.Column<int>(type: "int", nullable: false),
                    Bashliq = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Metn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Oxunub = table.Column<bool>(type: "bit", nullable: false),
                    OxunmaTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MezuniyyetId = table.Column<int>(type: "int", nullable: true),
                    IcazeId = table.Column<int>(type: "int", nullable: true),
                    MesajId = table.Column<int>(type: "int", nullable: true),
                    RedirectUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_Bildirisler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bildirisler_Isciler_IsciId",
                        column: x => x.IsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChatMesajlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GonderenIsciId = table.Column<int>(type: "int", nullable: false),
                    AlanIsciId = table.Column<int>(type: "int", nullable: false),
                    Metn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Oxunub = table.Column<bool>(type: "bit", nullable: false),
                    GonderilmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OxunmaTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TopluMesajGrupId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FaylAdi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FaylYolu = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FaylOlcusu = table.Column<long>(type: "bigint", nullable: true),
                    FaylTipi = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_ChatMesajlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatMesajlar_Isciler_AlanIsciId",
                        column: x => x.AlanIsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ChatMesajlar_Isciler_GonderenIsciId",
                        column: x => x.GonderenIsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Davamiyyetler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsciId = table.Column<int>(type: "int", nullable: false),
                    Tarix = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GirisVaxti = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CixisVaxti = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_Davamiyyetler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Davamiyyetler_Isciler_IsciId",
                        column: x => x.IsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Elanlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Bashliq = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Metn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GonderenIsciId = table.Column<int>(type: "int", nullable: false),
                    Vacibdir = table.Column<bool>(type: "bit", nullable: false),
                    BitirmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Aktivdir = table.Column<bool>(type: "bit", nullable: false),
                    SekilYolu = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FaylAdi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FaylYolu = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FaylTipi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FaylOlcusu = table.Column<long>(type: "bigint", nullable: true),
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
                    table.PrimaryKey("PK_Elanlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Elanlar_Isciler_GonderenIsciId",
                        column: x => x.GonderenIsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Gorushler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Bashliq = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Agenda = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TeshkilatciIsciId = table.Column<int>(type: "int", nullable: false),
                    Tarix = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BaslamaSaati = table.Column<TimeSpan>(type: "time", nullable: false),
                    BitisSaati = table.Column<TimeSpan>(type: "time", nullable: false),
                    Yer = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OnlineLink = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Nov = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Qeydler = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_Gorushler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Gorushler_Isciler_TeshkilatciIsciId",
                        column: x => x.TeshkilatciIsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HesabatSablonlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ad = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tesvir = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tezlik = table.Column<int>(type: "int", nullable: false),
                    KateqoriyaId = table.Column<int>(type: "int", nullable: false),
                    Prioritet = table.Column<int>(type: "int", nullable: false),
                    SonTarixGunu = table.Column<int>(type: "int", nullable: false),
                    SonTarixSaati = table.Column<TimeSpan>(type: "time", nullable: false),
                    MesulIsciId = table.Column<int>(type: "int", nullable: false),
                    DepartamentId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_HesabatSablonlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HesabatSablonlari_Departament_DepartamentId",
                        column: x => x.DepartamentId,
                        principalTable: "Departament",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HesabatSablonlari_HesabatKateqoriyalari_KateqoriyaId",
                        column: x => x.KateqoriyaId,
                        principalTable: "HesabatKateqoriyalari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HesabatSablonlari_Isciler_MesulIsciId",
                        column: x => x.MesulIsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Xatirlatmalar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsciId = table.Column<int>(type: "int", nullable: false),
                    Bashliq = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Qeyd = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    XatirlatmaTarixi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OxunubMu = table.Column<bool>(type: "bit", nullable: false),
                    GonderiilibMi = table.Column<bool>(type: "bit", nullable: false),
                    Nov = table.Column<int>(type: "int", nullable: false),
                    EntityTipi = table.Column<int>(type: "int", nullable: true),
                    EntityId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_Xatirlatmalar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Xatirlatmalar_Isciler_IsciId",
                        column: x => x.IsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Xercler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsciId = table.Column<int>(type: "int", nullable: false),
                    KateqoriyaId = table.Column<int>(type: "int", nullable: false),
                    Tesvir = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Mebleg = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    XercTarixi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    QebzFaylYolu = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TesdiqleyenIsciId = table.Column<int>(type: "int", nullable: true),
                    TesdiqTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ImtinaSebebi = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_Xercler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Xercler_Isciler_IsciId",
                        column: x => x.IsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Xercler_Isciler_TesdiqleyenIsciId",
                        column: x => x.TesdiqleyenIsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Xercler_XercKateqoriyalari_KateqoriyaId",
                        column: x => x.KateqoriyaId,
                        principalTable: "XercKateqoriyalari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Xestelikler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsciId = table.Column<int>(type: "int", nullable: false),
                    BaslamaTarixi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BitmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsGunSayi = table.Column<int>(type: "int", nullable: false),
                    BulletenNomresi = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MualiceMuessisesi = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Qeyd = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    HrId = table.Column<int>(type: "int", nullable: true),
                    HrTesdiqTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_Xestelikler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Xestelikler_Isciler_IsciId",
                        column: x => x.IsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Icazeler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsciId = table.Column<int>(type: "int", nullable: false),
                    EvezEdenIsciId = table.Column<int>(type: "int", nullable: true),
                    IcazeTarixi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BaslamaSaati = table.Column<TimeSpan>(type: "time", nullable: false),
                    BitisSaati = table.Column<TimeSpan>(type: "time", nullable: false),
                    Sebeb = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImtinaSebebi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SobeReisiTesdiq = table.Column<bool>(type: "bit", nullable: true),
                    SobeReisiId = table.Column<int>(type: "int", nullable: true),
                    SobeReisiTesdiqTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RehberTesdiq = table.Column<bool>(type: "bit", nullable: true),
                    RehberId = table.Column<int>(type: "int", nullable: true),
                    RehberTesdiqTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HrTesdiq = table.Column<bool>(type: "bit", nullable: true),
                    HrId = table.Column<int>(type: "int", nullable: true),
                    HrTesdiqTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_Icazeler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Icazeler_Isciler_EvezEdenIsciId",
                        column: x => x.EvezEdenIsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Icazeler_Isciler_HrId",
                        column: x => x.HrId,
                        principalTable: "Isciler",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Icazeler_Isciler_IsciId",
                        column: x => x.IsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Icazeler_Isciler_RehberId",
                        column: x => x.RehberId,
                        principalTable: "Isciler",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Icazeler_Isciler_SobeReisiId",
                        column: x => x.SobeReisiId,
                        principalTable: "Isciler",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "IsciAyliqQazanclar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsciId = table.Column<int>(type: "int", nullable: false),
                    Il = table.Column<int>(type: "int", nullable: false),
                    Ay = table.Column<int>(type: "int", nullable: false),
                    Qazanc = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ElIleDaxilEdilib = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_IsciAyliqQazanclar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IsciAyliqQazanclar_Isciler_IsciId",
                        column: x => x.IsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IsciGuzestler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsciId = table.Column<int>(type: "int", nullable: false),
                    GuzestId = table.Column<int>(type: "int", nullable: false),
                    BaslamaTarixi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BitmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_IsciGuzestler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IsciGuzestler_Guzestler_GuzestId",
                        column: x => x.GuzestId,
                        principalTable: "Guzestler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IsciGuzestler_Isciler_IsciId",
                        column: x => x.IsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IsciHYSler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsciId = table.Column<int>(type: "int", nullable: false),
                    Mebleg = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BaslamaTarixi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BitmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Qeyd = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_IsciHYSler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IsciHYSler_Isciler_IsciId",
                        column: x => x.IsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IsciMaasTarixceleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsciId = table.Column<int>(type: "int", nullable: false),
                    KohneMaas = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    YeniMaas = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DeyismeTarixi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EmrinNomresi = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Sebeb = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
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
                    table.PrimaryKey("PK_IsciMaasTarixceleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IsciMaasTarixceleri_Isciler_IsciId",
                        column: x => x.IsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IsciMaliyeleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsciId = table.Column<int>(type: "int", nullable: false),
                    CariMaas = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BankHesabNo = table.Column<string>(type: "nvarchar(34)", maxLength: 34, nullable: true),
                    SosialSigortaNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
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
                    table.PrimaryKey("PK_IsciMaliyeleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IsciMaliyeleri_Isciler_IsciId",
                        column: x => x.IsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IsciStrukturRollari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsciId = table.Column<int>(type: "int", nullable: false),
                    RolTipi = table.Column<int>(type: "int", nullable: false),
                    DepartamentId = table.Column<int>(type: "int", nullable: true),
                    BaslamaTarixi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BitmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_IsciStrukturRollari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IsciStrukturRollari_Departament_DepartamentId",
                        column: x => x.DepartamentId,
                        principalTable: "Departament",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_IsciStrukturRollari_Isciler_IsciId",
                        column: x => x.IsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "KreditMuracietler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdSoyadAtaAdi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FIN = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    KreditMeblegi = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Valyuta = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    KreditMuddeti = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsYeri = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmekHaqqi = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Telefon = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Meqsed = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MuracietTarixi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IP = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MailMessageId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Menbe = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Qeyd = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BaxanIsciId = table.Column<int>(type: "int", nullable: true),
                    BaxilmaTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    KomiteQerari = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    KomiteProtokolNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    KomiteQerarTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TesdiqMebleg = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TesdiqMuddet = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FaizDerecesi = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Teminat = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_KreditMuracietler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KreditMuracietler_Isciler_BaxanIsciId",
                        column: x => x.BaxanIsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Maaslar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsciId = table.Column<int>(type: "int", nullable: false),
                    Il = table.Column<int>(type: "int", nullable: false),
                    Ay = table.Column<int>(type: "int", nullable: false),
                    HesablanmaTarixi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TesdiqTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OdenisTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BrutMebleg = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NetMebleg = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_Maaslar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Maaslar_Isciler_IsciId",
                        column: x => x.IsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Mesajlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GonderenIsciId = table.Column<int>(type: "int", nullable: false),
                    AlanIsciId = table.Column<int>(type: "int", nullable: false),
                    Movzu = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Metn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Oxunub = table.Column<bool>(type: "bit", nullable: false),
                    OxunmaTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CavabVerdigiMesajId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_Mesajlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Mesajlar_Isciler_AlanIsciId",
                        column: x => x.AlanIsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Mesajlar_Isciler_GonderenIsciId",
                        column: x => x.GonderenIsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Mesajlar_Mesajlar_CavabVerdigiMesajId",
                        column: x => x.CavabVerdigiMesajId,
                        principalTable: "Mesajlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MezuniyyetBalanslari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsciId = table.Column<int>(type: "int", nullable: false),
                    Il = table.Column<int>(type: "int", nullable: false),
                    Nov = table.Column<int>(type: "int", nullable: false),
                    ToplamGun = table.Column<int>(type: "int", nullable: false),
                    IstifadeOlunanGun = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_MezuniyyetBalanslari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MezuniyyetBalanslari_Isciler_IsciId",
                        column: x => x.IsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Mezuniyyetler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsciId = table.Column<int>(type: "int", nullable: false),
                    EvezEdenIsciId = table.Column<int>(type: "int", nullable: true),
                    Nov = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    BaslamaTarixi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BitmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsGunlerininSayi = table.Column<int>(type: "int", nullable: false),
                    Qeyd = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImtinaSebebi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SobeReisiTesdiq = table.Column<bool>(type: "bit", nullable: true),
                    SobeReisiId = table.Column<int>(type: "int", nullable: true),
                    SobeReisiTesdiqTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RehberTesdiq = table.Column<bool>(type: "bit", nullable: true),
                    RehberId = table.Column<int>(type: "int", nullable: true),
                    RehberTesdiqTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HrTesdiq = table.Column<bool>(type: "bit", nullable: true),
                    HrId = table.Column<int>(type: "int", nullable: true),
                    HrTesdiqTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OdenisTipi = table.Column<int>(type: "int", nullable: false),
                    OdenisStatus = table.Column<int>(type: "int", nullable: false),
                    OdenenMebleg = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    OdenilmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OdeyenMuhasibId = table.Column<int>(type: "int", nullable: true),
                    PlanliOdenisTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_Mezuniyyetler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Mezuniyyetler_Isciler_EvezEdenIsciId",
                        column: x => x.EvezEdenIsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Mezuniyyetler_Isciler_HrId",
                        column: x => x.HrId,
                        principalTable: "Isciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Mezuniyyetler_Isciler_IsciId",
                        column: x => x.IsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Mezuniyyetler_Isciler_OdeyenMuhasibId",
                        column: x => x.OdeyenMuhasibId,
                        principalTable: "Isciler",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Mezuniyyetler_Isciler_RehberId",
                        column: x => x.RehberId,
                        principalTable: "Isciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Mezuniyyetler_Isciler_SobeReisiId",
                        column: x => x.SobeReisiId,
                        principalTable: "Isciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PerformansQiymetlendirmeler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsciId = table.Column<int>(type: "int", nullable: false),
                    QiymetlendirenIsciId = table.Column<int>(type: "int", nullable: false),
                    DovrTipi = table.Column<int>(type: "int", nullable: false),
                    Il = table.Column<int>(type: "int", nullable: false),
                    Rubu = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsciOrtalamaQiymet = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    MudirOrtalamaQiymet = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    YekunQiymet = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    IsciSherhi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MudirSherhi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InkisafPlani = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsciQiymetlendirmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MudirQiymetlendirmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_PerformansQiymetlendirmeler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PerformansQiymetlendirmeler_Isciler_IsciId",
                        column: x => x.IsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PerformansQiymetlendirmeler_Isciler_QiymetlendirenIsciId",
                        column: x => x.QiymetlendirenIsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Sertifikatlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsciId = table.Column<int>(type: "int", nullable: false),
                    Ad = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VerenQurum = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VerilmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BitirmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FaylYolu = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_Sertifikatlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sertifikatlar_Isciler_IsciId",
                        column: x => x.IsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Tapshiriqlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Bashliq = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tesvir = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    YaradanIsciId = table.Column<int>(type: "int", nullable: false),
                    TeyinOlunanIsciId = table.Column<int>(type: "int", nullable: false),
                    SonTarix = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Prioritet = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TamamlanmaFaizi = table.Column<int>(type: "int", nullable: false),
                    Qeyd = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_Tapshiriqlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tapshiriqlar_Isciler_TeyinOlunanIsciId",
                        column: x => x.TeyinOlunanIsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Tapshiriqlar_Isciler_YaradanIsciId",
                        column: x => x.YaradanIsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TelimIshtiraklar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TelimId = table.Column<int>(type: "int", nullable: false),
                    IsciId = table.Column<int>(type: "int", nullable: false),
                    Istirakdir = table.Column<bool>(type: "bit", nullable: false),
                    Qiymet = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    Qeyd = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_TelimIshtiraklar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TelimIshtiraklar_Isciler_IsciId",
                        column: x => x.IsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TelimIshtiraklar_Telimler_TelimId",
                        column: x => x.TelimId,
                        principalTable: "Telimler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Senedler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    SenedNovuId = table.Column<int>(type: "int", nullable: false),
                    Basliq = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AcarSoz = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SenedNomresi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Mexfilik = table.Column<int>(type: "int", nullable: false),
                    ReferenceType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReferenceId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_Senedler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Senedler_Departament_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departament",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Senedler_SenedNovleri_SenedNovuId",
                        column: x => x.SenedNovuId,
                        principalTable: "SenedNovleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SenedSablonlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ad = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tesvir = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SenedNovuId = table.Column<int>(type: "int", nullable: false),
                    FaylYolu = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FaylAdi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Aktiv = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_SenedSablonlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SenedSablonlar_SenedNovleri_SenedNovuId",
                        column: x => x.SenedNovuId,
                        principalTable: "SenedNovleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IsciTeyinatlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsciId = table.Column<int>(type: "int", nullable: false),
                    DepartamentId = table.Column<int>(type: "int", nullable: false),
                    VezifeId = table.Column<int>(type: "int", nullable: false),
                    BaslamaTarixi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BitmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Esasdir = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_IsciTeyinatlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IsciTeyinatlari_Departament_DepartamentId",
                        column: x => x.DepartamentId,
                        principalTable: "Departament",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_IsciTeyinatlari_Isciler_IsciId",
                        column: x => x.IsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_IsciTeyinatlari_Vezifeler_VezifeId",
                        column: x => x.VezifeId,
                        principalTable: "Vezifeler",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "OdenisTapsiriqlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nomre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tarix = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OduyenBankId = table.Column<int>(type: "int", nullable: false),
                    OduyenMusteriId = table.Column<int>(type: "int", nullable: false),
                    OduyenHesabId = table.Column<int>(type: "int", nullable: false),
                    AlanBankId = table.Column<int>(type: "int", nullable: false),
                    AlanMusteriId = table.Column<int>(type: "int", nullable: false),
                    AlanHesabId = table.Column<int>(type: "int", nullable: false),
                    Mebleg = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ValyutaId = table.Column<int>(type: "int", nullable: false),
                    MeblegYazi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Teyinat = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ElaveInformasiya = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BudceTesnifatininKodu = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BudceSeviyyesininKodu = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_OdenisTapsiriqlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OdenisTapsiriqlari_Banklar_AlanBankId",
                        column: x => x.AlanBankId,
                        principalTable: "Banklar",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OdenisTapsiriqlari_Banklar_OduyenBankId",
                        column: x => x.OduyenBankId,
                        principalTable: "Banklar",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OdenisTapsiriqlari_MusteriHesablari_AlanHesabId",
                        column: x => x.AlanHesabId,
                        principalTable: "MusteriHesablari",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OdenisTapsiriqlari_MusteriHesablari_OduyenHesabId",
                        column: x => x.OduyenHesabId,
                        principalTable: "MusteriHesablari",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OdenisTapsiriqlari_Musteriler_AlanMusteriId",
                        column: x => x.AlanMusteriId,
                        principalTable: "Musteriler",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OdenisTapsiriqlari_Musteriler_OduyenMusteriId",
                        column: x => x.OduyenMusteriId,
                        principalTable: "Musteriler",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OdenisTapsiriqlari_Valyutalar_ValyutaId",
                        column: x => x.ValyutaId,
                        principalTable: "Valyutalar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GorushIshtirakcilar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GorushId = table.Column<int>(type: "int", nullable: false),
                    IsciId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Qeyd = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_GorushIshtirakcilar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GorushIshtirakcilar_Gorushler_GorushId",
                        column: x => x.GorushId,
                        principalTable: "Gorushler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GorushIshtirakcilar_Isciler_IsciId",
                        column: x => x.IsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HesabatTapshiriqlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SablonId = table.Column<int>(type: "int", nullable: false),
                    DovrBaslangic = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DovrSon = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Deadline = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IcraEdenIsciId = table.Column<int>(type: "int", nullable: true),
                    IcraTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Qeyd = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_HesabatTapshiriqlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HesabatTapshiriqlari_HesabatSablonlari_SablonId",
                        column: x => x.SablonId,
                        principalTable: "HesabatSablonlari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HesabatTapshiriqlari_Isciler_IcraEdenIsciId",
                        column: x => x.IcraEdenIsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "XestelikOdenisleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    XestelikId = table.Column<int>(type: "int", nullable: false),
                    IsciId = table.Column<int>(type: "int", nullable: false),
                    Il = table.Column<int>(type: "int", nullable: false),
                    Ay = table.Column<int>(type: "int", nullable: false),
                    BirGunluk = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    SirketGunSayi = table.Column<int>(type: "int", nullable: false),
                    DsmfGunSayi = table.Column<int>(type: "int", nullable: false),
                    SirketOdenis = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DsmfOdenis = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MaasId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_XestelikOdenisleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_XestelikOdenisleri_Isciler_IsciId",
                        column: x => x.IsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_XestelikOdenisleri_Maaslar_MaasId",
                        column: x => x.MaasId,
                        principalTable: "Maaslar",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_XestelikOdenisleri_Xestelikler_XestelikId",
                        column: x => x.XestelikId,
                        principalTable: "Xestelikler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MaasDetaylari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaasId = table.Column<int>(type: "int", nullable: false),
                    MaasNovuId = table.Column<int>(type: "int", nullable: false),
                    Mebleg = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Aciqlama = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_MaasDetaylari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaasDetaylari_MaasNovleri_MaasNovuId",
                        column: x => x.MaasNovuId,
                        principalTable: "MaasNovleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaasDetaylari_Maaslar_MaasId",
                        column: x => x.MaasId,
                        principalTable: "Maaslar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EvezediciTesdiqler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MezuniyyetId = table.Column<int>(type: "int", nullable: false),
                    EvezediciIsciId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Qeyd = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CavabTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_EvezediciTesdiqler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvezediciTesdiqler_Isciler_EvezediciIsciId",
                        column: x => x.EvezediciIsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EvezediciTesdiqler_Mezuniyyetler_MezuniyyetId",
                        column: x => x.MezuniyyetId,
                        principalTable: "Mezuniyyetler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PerformansKriteriyalar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PerformansId = table.Column<int>(type: "int", nullable: false),
                    KriteriyaAdi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Ceki = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    IsciQiymeti = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    MudirQiymeti = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    IsciSherhi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MudirSherhi = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_PerformansKriteriyalar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PerformansKriteriyalar_PerformansQiymetlendirmeler_PerformansId",
                        column: x => x.PerformansId,
                        principalTable: "PerformansQiymetlendirmeler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TapshiriqSherhler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TapshiriqId = table.Column<int>(type: "int", nullable: false),
                    MuellifIsciId = table.Column<int>(type: "int", nullable: false),
                    Metn = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_TapshiriqSherhler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TapshiriqSherhler_Isciler_MuellifIsciId",
                        column: x => x.MuellifIsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TapshiriqSherhler_Tapshiriqlar_TapshiriqId",
                        column: x => x.TapshiriqId,
                        principalTable: "Tapshiriqlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SenedId = table.Column<int>(type: "int", nullable: true),
                    Ip = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DetailsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Senedler_SenedId",
                        column: x => x.SenedId,
                        principalTable: "Senedler",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SenedAccessler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SenedId = table.Column<int>(type: "int", nullable: false),
                    PrincipalType = table.Column<int>(type: "int", nullable: false),
                    PrincipalId = table.Column<int>(type: "int", nullable: false),
                    Permission = table.Column<int>(type: "int", nullable: false),
                    SenedId1 = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_SenedAccessler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SenedAccessler_Senedler_SenedId",
                        column: x => x.SenedId,
                        principalTable: "Senedler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SenedAccessler_Senedler_SenedId1",
                        column: x => x.SenedId1,
                        principalTable: "Senedler",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SenedFayllar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SenedId = table.Column<int>(type: "int", nullable: false),
                    VersiyaNo = table.Column<int>(type: "int", nullable: false),
                    OriginalAd = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StoredAd = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OlcuBytes = table.Column<long>(type: "bigint", nullable: false),
                    Sha256 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Yol = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AktivVersiya = table.Column<bool>(type: "bit", nullable: false),
                    StorageProvider = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_SenedFayllar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SenedFayllar_Senedler_SenedId",
                        column: x => x.SenedId,
                        principalTable: "Senedler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SenedTagMaps",
                columns: table => new
                {
                    SenedId = table.Column<int>(type: "int", nullable: false),
                    TagId = table.Column<int>(type: "int", nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_SenedTagMaps", x => new { x.SenedId, x.TagId });
                    table.ForeignKey(
                        name: "FK_SenedTagMaps_Senedler_SenedId",
                        column: x => x.SenedId,
                        principalTable: "Senedler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SenedTagMaps_Tagler_TagId",
                        column: x => x.TagId,
                        principalTable: "Tagler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "MaasNovleri",
                columns: new[] { "Id", "Ad", "Aktivdir", "SilenIcraciId", "Silinib", "SilinmeTarixi", "Tip", "YaradanIcraciId", "YaradilmaTarixi", "YenilenmeTarixi", "YenileyenIcraciId" },
                values: new object[,]
                {
                    { 1, "Əsas Əməkhaqqı", true, null, false, null, 1, null, new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4160), null, null },
                    { 2, "Bonus/Mükafat", true, null, false, null, 1, null, new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4165), null, null },
                    { 3, "Məzuniyyət Ödənişi", true, null, false, null, 1, null, new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4165), null, null },
                    { 4, "Davamiyyət Kəsintisi", true, null, false, null, 2, null, new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4165), null, null },
                    { 5, "Gecikdirmə Cəriməsi", true, null, false, null, 2, null, new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4165), null, null },
                    { 6, "Gəlir Vergisi", true, null, false, null, 2, null, new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4165), null, null },
                    { 7, "DSMF (İşçi)", true, null, false, null, 2, null, new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4170), null, null },
                    { 8, "İşsizlik Sığortası (İşçi)", true, null, false, null, 2, null, new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4170), null, null },
                    { 9, "İTSS", true, null, false, null, 2, null, new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4170), null, null },
                    { 10, "DSMF (İşəgötürən)", true, null, false, null, 3, null, new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4170), null, null },
                    { 11, "İşsizlik Sığortası (İşəgötürən)", true, null, false, null, 3, null, new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4170), null, null },
                    { 12, "İTSS (İşəgötürən)", true, null, false, null, 3, null, new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4170), null, null },
                    { 13, "Xəstəlik Ödənişi", true, null, false, null, 1, null, new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4175), null, null }
                });

            migrationBuilder.InsertData(
                table: "MaasParametrleri",
                columns: new[] { "Id", "Aciqlama", "Aktivdir", "BaslamaTarixi", "BitmeTarixi", "Deyer", "Nov", "SilenIcraciId", "Silinib", "SilinmeTarixi", "Tip", "YaradanIcraciId", "YaradilmaTarixi", "YenilenmeTarixi", "YenileyenIcraciId" },
                values: new object[,]
                {
                    { 1, "2026", true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 14m, 1, null, false, null, 1, null, new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4194), null, null },
                    { 2, "2026", true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 3m, 2, null, false, null, 1, null, new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4199), null, null },
                    { 3, "2026", true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 0.5m, 3, null, false, null, 1, null, new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4204), null, null },
                    { 4, "2026", true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 2m, 4, null, false, null, 1, null, new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4204), null, null },
                    { 5, "2026", true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 345m, 5, null, false, null, 2, null, new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4209), null, null },
                    { 6, "2026", true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 200m, 6, null, false, null, 2, null, new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4209), null, null }
                });

            migrationBuilder.InsertData(
                table: "VergiPilleleri",
                columns: new[] { "Id", "Aciqlama", "Aktivdir", "AsagiHedd", "BaslamaTarixi", "BitmeTarixi", "Faiz", "Nov", "SabitMebleg", "SilenIcraciId", "Silinib", "SilinmeTarixi", "Sira", "YaradanIcraciId", "YaradilmaTarixi", "YenilenmeTarixi", "YenileyenIcraciId", "YuxariHedd" },
                values: new object[,]
                {
                    { 1, "2026: 0–2500 AZN → 3%", true, 0m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 3m, 1, 0m, null, false, null, 1, null, new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4233), null, null, 2500m },
                    { 2, "2026: 2500–8000 AZN → 75+10%", true, 2500m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 10m, 1, 75m, null, false, null, 2, null, new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4238), null, null, 8000m },
                    { 3, "2026: 8000+ AZN → 625+14%", true, 8000m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 14m, 1, 625m, null, false, null, 3, null, new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4238), null, null, null },
                    { 4, "2026: 0–200 AZN → 3%", true, 0m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 3m, 2, 0m, null, false, null, 1, null, new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4238), null, null, 200m },
                    { 5, "2026: 200+ AZN → 6+10%", true, 200m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 10m, 2, 6m, null, false, null, 2, null, new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4243), null, null, null },
                    { 6, "2026: 0–200 AZN → 22%", true, 0m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 22m, 7, 0m, null, false, null, 1, null, new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4243), null, null, 200m },
                    { 7, "2026: 200–8000 AZN → 44+15%", true, 200m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 15m, 7, 44m, null, false, null, 2, null, new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4243), null, null, 8000m },
                    { 8, "2026: 8000+ AZN → 1214+11%", true, 8000m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 11m, 7, 1214m, null, false, null, 3, null, new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4248), null, null, null },
                    { 9, "2026: 0–8000 AZN → 2%", true, 0m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 2m, 4, 0m, null, false, null, 1, null, new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4248), null, null, 8000m },
                    { 10, "2026: 8000+ AZN → 160+0.5%", true, 8000m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 0.5m, 4, 160m, null, false, null, 2, null, new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4272), null, null, null },
                    { 11, "2026: 0–8000 AZN → 2%", true, 0m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 2m, 9, 0m, null, false, null, 1, null, new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4277), null, null, 8000m },
                    { 12, "2026: 8000+ AZN → 160+0.5%", true, 8000m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 0.5m, 9, 160m, null, false, null, 2, null, new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4277), null, null, null }
                });

            migrationBuilder.InsertData(
                table: "XercKateqoriyalari",
                columns: new[] { "Id", "Ad", "Aktivdir", "Ikon", "SilenIcraciId", "Silinib", "SilinmeTarixi", "YaradanIcraciId", "YaradilmaTarixi", "YenilenmeTarixi", "YenileyenIcraciId" },
                values: new object[,]
                {
                    { 1, "Taksi", true, "bi-taxi-front", null, false, null, null, new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4010), null, null },
                    { 2, "Yemək", true, "bi-cup-hot", null, false, null, null, new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4025), null, null },
                    { 3, "Ofis ləvazimatı", true, "bi-printer", null, false, null, null, new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4025), null, null },
                    { 4, "Səfər xərcləri", true, "bi-airplane", null, false, null, null, new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4029), null, null },
                    { 5, "Digər", true, "bi-three-dots", null, false, null, null, new DateTime(2026, 4, 20, 14, 14, 7, 62, DateTimeKind.Local).AddTicks(4029), null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_SenedId",
                table: "AuditLogs",
                column: "SenedId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Avanslar_IsciId",
                table: "Avanslar",
                column: "IsciId");

            migrationBuilder.CreateIndex(
                name: "IX_Avanslar_MuhasibId",
                table: "Avanslar",
                column: "MuhasibId");

            migrationBuilder.CreateIndex(
                name: "IX_BankHesablari_BankId",
                table: "BankHesablari",
                column: "BankId");

            migrationBuilder.CreateIndex(
                name: "IX_BankHesablari_ValyutaId",
                table: "BankHesablari",
                column: "ValyutaId");

            migrationBuilder.CreateIndex(
                name: "IX_Bildirisler_IsciId",
                table: "Bildirisler",
                column: "IsciId");

            migrationBuilder.CreateIndex(
                name: "IX_Budceler_DepartamentId_Il_Ay",
                table: "Budceler",
                columns: new[] { "DepartamentId", "Il", "Ay" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChatMesajlar_AlanIsciId",
                table: "ChatMesajlar",
                column: "AlanIsciId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMesajlar_GonderenIsciId",
                table: "ChatMesajlar",
                column: "GonderenIsciId");

            migrationBuilder.CreateIndex(
                name: "IX_Davamiyyetler_IsciId_Tarix",
                table: "Davamiyyetler",
                columns: new[] { "IsciId", "Tarix" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Elanlar_GonderenIsciId",
                table: "Elanlar",
                column: "GonderenIsciId");

            migrationBuilder.CreateIndex(
                name: "IX_EvezediciTesdiqler_EvezediciIsciId",
                table: "EvezediciTesdiqler",
                column: "EvezediciIsciId");

            migrationBuilder.CreateIndex(
                name: "IX_EvezediciTesdiqler_MezuniyyetId",
                table: "EvezediciTesdiqler",
                column: "MezuniyyetId");

            migrationBuilder.CreateIndex(
                name: "IX_GorushIshtirakcilar_GorushId",
                table: "GorushIshtirakcilar",
                column: "GorushId");

            migrationBuilder.CreateIndex(
                name: "IX_GorushIshtirakcilar_IsciId",
                table: "GorushIshtirakcilar",
                column: "IsciId");

            migrationBuilder.CreateIndex(
                name: "IX_Gorushler_TeshkilatciIsciId",
                table: "Gorushler",
                column: "TeshkilatciIsciId");

            migrationBuilder.CreateIndex(
                name: "IX_Guzestler_Aktivdir",
                table: "Guzestler",
                column: "Aktivdir");

            migrationBuilder.CreateIndex(
                name: "IX_HesabatSablonlari_DepartamentId",
                table: "HesabatSablonlari",
                column: "DepartamentId");

            migrationBuilder.CreateIndex(
                name: "IX_HesabatSablonlari_KateqoriyaId",
                table: "HesabatSablonlari",
                column: "KateqoriyaId");

            migrationBuilder.CreateIndex(
                name: "IX_HesabatSablonlari_MesulIsciId",
                table: "HesabatSablonlari",
                column: "MesulIsciId");

            migrationBuilder.CreateIndex(
                name: "IX_HesabatTapshiriqlari_IcraEdenIsciId",
                table: "HesabatTapshiriqlari",
                column: "IcraEdenIsciId");

            migrationBuilder.CreateIndex(
                name: "IX_HesabatTapshiriqlari_SablonId",
                table: "HesabatTapshiriqlari",
                column: "SablonId");

            migrationBuilder.CreateIndex(
                name: "IX_Xatirlatmalar_IsciId",
                table: "Xatirlatmalar",
                column: "IsciId");

            migrationBuilder.CreateIndex(
                name: "IX_XercKateqoriyalari_Ad",
                table: "XercKateqoriyalari",
                column: "Ad",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Xercler_IsciId",
                table: "Xercler",
                column: "IsciId");

            migrationBuilder.CreateIndex(
                name: "IX_Xercler_KateqoriyaId",
                table: "Xercler",
                column: "KateqoriyaId");

            migrationBuilder.CreateIndex(
                name: "IX_Xercler_TesdiqleyenIsciId",
                table: "Xercler",
                column: "TesdiqleyenIsciId");

            migrationBuilder.CreateIndex(
                name: "IX_Xestelikler_IsciId_BaslamaTarixi",
                table: "Xestelikler",
                columns: new[] { "IsciId", "BaslamaTarixi" });

            migrationBuilder.CreateIndex(
                name: "IX_XestelikOdenisleri_XestelikId",
                table: "XestelikOdenisleri",
                column: "XestelikId");

            migrationBuilder.CreateIndex(
                name: "IX_XestelikOdenisleri_IsciId_Il_Ay",
                table: "XestelikOdenisleri",
                columns: new[] { "IsciId", "Il", "Ay" });

            migrationBuilder.CreateIndex(
                name: "IX_XestelikOdenisleri_MaasId",
                table: "XestelikOdenisleri",
                column: "MaasId");

            migrationBuilder.CreateIndex(
                name: "IX_Icazeler_EvezEdenIsciId",
                table: "Icazeler",
                column: "EvezEdenIsciId");

            migrationBuilder.CreateIndex(
                name: "IX_Icazeler_HrId",
                table: "Icazeler",
                column: "HrId");

            migrationBuilder.CreateIndex(
                name: "IX_Icazeler_IsciId",
                table: "Icazeler",
                column: "IsciId");

            migrationBuilder.CreateIndex(
                name: "IX_Icazeler_RehberId",
                table: "Icazeler",
                column: "RehberId");

            migrationBuilder.CreateIndex(
                name: "IX_Icazeler_SobeReisiId",
                table: "Icazeler",
                column: "SobeReisiId");

            migrationBuilder.CreateIndex(
                name: "IX_IsciAyliqQazanclar_IsciId_Il_Ay",
                table: "IsciAyliqQazanclar",
                columns: new[] { "IsciId", "Il", "Ay" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IsciGuzestler_GuzestId",
                table: "IsciGuzestler",
                column: "GuzestId");

            migrationBuilder.CreateIndex(
                name: "IX_IsciGuzestler_IsciId_GuzestId",
                table: "IsciGuzestler",
                columns: new[] { "IsciId", "GuzestId" });

            migrationBuilder.CreateIndex(
                name: "IX_IsciHYSler_IsciId",
                table: "IsciHYSler",
                column: "IsciId");

            migrationBuilder.CreateIndex(
                name: "IX_Isciler_AppUserId",
                table: "Isciler",
                column: "AppUserId",
                unique: true,
                filter: "[AppUserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Isciler_FIN",
                table: "Isciler",
                column: "FIN",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IsciMaasTarixceleri_IsciId",
                table: "IsciMaasTarixceleri",
                column: "IsciId");

            migrationBuilder.CreateIndex(
                name: "IX_IsciMaliyeleri_IsciId",
                table: "IsciMaliyeleri",
                column: "IsciId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IsciStrukturRollari_DepartamentId",
                table: "IsciStrukturRollari",
                column: "DepartamentId");

            migrationBuilder.CreateIndex(
                name: "IX_IsciStrukturRollari_IsciId",
                table: "IsciStrukturRollari",
                column: "IsciId");

            migrationBuilder.CreateIndex(
                name: "IX_IsciTeyinatlari_DepartamentId",
                table: "IsciTeyinatlari",
                column: "DepartamentId");

            migrationBuilder.CreateIndex(
                name: "IX_IsciTeyinatlari_IsciId",
                table: "IsciTeyinatlari",
                column: "IsciId");

            migrationBuilder.CreateIndex(
                name: "IX_IsciTeyinatlari_VezifeId",
                table: "IsciTeyinatlari",
                column: "VezifeId");

            migrationBuilder.CreateIndex(
                name: "IX_KreditMuracietler_BaxanIsciId",
                table: "KreditMuracietler",
                column: "BaxanIsciId");

            migrationBuilder.CreateIndex(
                name: "IX_LoginLogs_LoginTime",
                table: "LoginLogs",
                column: "LoginTime");

            migrationBuilder.CreateIndex(
                name: "IX_MaasDetaylari_MaasId",
                table: "MaasDetaylari",
                column: "MaasId");

            migrationBuilder.CreateIndex(
                name: "IX_MaasDetaylari_MaasNovuId",
                table: "MaasDetaylari",
                column: "MaasNovuId");

            migrationBuilder.CreateIndex(
                name: "IX_Maaslar_IsciId_Il_Ay",
                table: "Maaslar",
                columns: new[] { "IsciId", "Il", "Ay" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaasNovleri_Ad",
                table: "MaasNovleri",
                column: "Ad",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaasParametrleri_Nov_BaslamaTarixi",
                table: "MaasParametrleri",
                columns: new[] { "Nov", "BaslamaTarixi" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Mesajlar_AlanIsciId",
                table: "Mesajlar",
                column: "AlanIsciId");

            migrationBuilder.CreateIndex(
                name: "IX_Mesajlar_CavabVerdigiMesajId",
                table: "Mesajlar",
                column: "CavabVerdigiMesajId");

            migrationBuilder.CreateIndex(
                name: "IX_Mesajlar_GonderenIsciId",
                table: "Mesajlar",
                column: "GonderenIsciId");

            migrationBuilder.CreateIndex(
                name: "IX_MezuniyyetBalanslari_IsciId_Il_Nov",
                table: "MezuniyyetBalanslari",
                columns: new[] { "IsciId", "Il", "Nov" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Mezuniyyetler_EvezEdenIsciId",
                table: "Mezuniyyetler",
                column: "EvezEdenIsciId");

            migrationBuilder.CreateIndex(
                name: "IX_Mezuniyyetler_HrId",
                table: "Mezuniyyetler",
                column: "HrId");

            migrationBuilder.CreateIndex(
                name: "IX_Mezuniyyetler_IsciId",
                table: "Mezuniyyetler",
                column: "IsciId");

            migrationBuilder.CreateIndex(
                name: "IX_Mezuniyyetler_OdeyenMuhasibId",
                table: "Mezuniyyetler",
                column: "OdeyenMuhasibId");

            migrationBuilder.CreateIndex(
                name: "IX_Mezuniyyetler_RehberId",
                table: "Mezuniyyetler",
                column: "RehberId");

            migrationBuilder.CreateIndex(
                name: "IX_Mezuniyyetler_SobeReisiId",
                table: "Mezuniyyetler",
                column: "SobeReisiId");

            migrationBuilder.CreateIndex(
                name: "IX_MusteriHesablari_MusteriId_Iban",
                table: "MusteriHesablari",
                columns: new[] { "MusteriId", "Iban" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MusteriHesablari_ValyutaId",
                table: "MusteriHesablari",
                column: "ValyutaId");

            migrationBuilder.CreateIndex(
                name: "IX_OdenisTapsiriqlari_AlanBankId",
                table: "OdenisTapsiriqlari",
                column: "AlanBankId");

            migrationBuilder.CreateIndex(
                name: "IX_OdenisTapsiriqlari_AlanHesabId",
                table: "OdenisTapsiriqlari",
                column: "AlanHesabId");

            migrationBuilder.CreateIndex(
                name: "IX_OdenisTapsiriqlari_AlanMusteriId",
                table: "OdenisTapsiriqlari",
                column: "AlanMusteriId");

            migrationBuilder.CreateIndex(
                name: "IX_OdenisTapsiriqlari_OduyenBankId",
                table: "OdenisTapsiriqlari",
                column: "OduyenBankId");

            migrationBuilder.CreateIndex(
                name: "IX_OdenisTapsiriqlari_OduyenHesabId",
                table: "OdenisTapsiriqlari",
                column: "OduyenHesabId");

            migrationBuilder.CreateIndex(
                name: "IX_OdenisTapsiriqlari_OduyenMusteriId",
                table: "OdenisTapsiriqlari",
                column: "OduyenMusteriId");

            migrationBuilder.CreateIndex(
                name: "IX_OdenisTapsiriqlari_ValyutaId",
                table: "OdenisTapsiriqlari",
                column: "ValyutaId");

            migrationBuilder.CreateIndex(
                name: "IX_PerformansKriteriyalar_PerformansId",
                table: "PerformansKriteriyalar",
                column: "PerformansId");

            migrationBuilder.CreateIndex(
                name: "IX_PerformansQiymetlendirmeler_IsciId",
                table: "PerformansQiymetlendirmeler",
                column: "IsciId");

            migrationBuilder.CreateIndex(
                name: "IX_PerformansQiymetlendirmeler_QiymetlendirenIsciId",
                table: "PerformansQiymetlendirmeler",
                column: "QiymetlendirenIsciId");

            migrationBuilder.CreateIndex(
                name: "IX_SenedAccessler_SenedId",
                table: "SenedAccessler",
                column: "SenedId");

            migrationBuilder.CreateIndex(
                name: "IX_SenedAccessler_SenedId1",
                table: "SenedAccessler",
                column: "SenedId1");

            migrationBuilder.CreateIndex(
                name: "IX_senedDovriyyesiIstifadeciIcazeleri_DepartamentId",
                table: "senedDovriyyesiIstifadeciIcazeleri",
                column: "DepartamentId");

            migrationBuilder.CreateIndex(
                name: "IX_senedDovriyyesiIstifadeciIcazeleri_IstifadeciId",
                table: "senedDovriyyesiIstifadeciIcazeleri",
                column: "IstifadeciId");

            migrationBuilder.CreateIndex(
                name: "IX_SenedFayllar_SenedId_VersiyaNo",
                table: "SenedFayllar",
                columns: new[] { "SenedId", "VersiyaNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Senedler_DepartmentId",
                table: "Senedler",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Senedler_SenedNovuId",
                table: "Senedler",
                column: "SenedNovuId");

            migrationBuilder.CreateIndex(
                name: "IX_SenedNovleri_DepartmentId",
                table: "SenedNovleri",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_SenedSablonlar_SenedNovuId",
                table: "SenedSablonlar",
                column: "SenedNovuId");

            migrationBuilder.CreateIndex(
                name: "IX_SenedTagMaps_TagId",
                table: "SenedTagMaps",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_Sertifikatlar_IsciId",
                table: "Sertifikatlar",
                column: "IsciId");

            migrationBuilder.CreateIndex(
                name: "IX_Tapshiriqlar_TeyinOlunanIsciId",
                table: "Tapshiriqlar",
                column: "TeyinOlunanIsciId");

            migrationBuilder.CreateIndex(
                name: "IX_Tapshiriqlar_YaradanIsciId",
                table: "Tapshiriqlar",
                column: "YaradanIsciId");

            migrationBuilder.CreateIndex(
                name: "IX_TapshiriqSherhler_MuellifIsciId",
                table: "TapshiriqSherhler",
                column: "MuellifIsciId");

            migrationBuilder.CreateIndex(
                name: "IX_TapshiriqSherhler_TapshiriqId",
                table: "TapshiriqSherhler",
                column: "TapshiriqId");

            migrationBuilder.CreateIndex(
                name: "IX_TelimIshtiraklar_IsciId",
                table: "TelimIshtiraklar",
                column: "IsciId");

            migrationBuilder.CreateIndex(
                name: "IX_TelimIshtiraklar_TelimId",
                table: "TelimIshtiraklar",
                column: "TelimId");

            migrationBuilder.CreateIndex(
                name: "IX_UserDepartments_DepartmentId",
                table: "UserDepartments",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_UserDepartments_UserId",
                table: "UserDepartments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_PermissionId",
                table: "UserPermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_UserId",
                table: "UserPermissions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_VergiPilleleri_Nov_Aktivdir",
                table: "VergiPilleleri",
                columns: new[] { "Nov", "Aktivdir" });

            migrationBuilder.CreateIndex(
                name: "IX_Vezifeler_Ad_DepartamentId",
                table: "Vezifeler",
                columns: new[] { "Ad", "DepartamentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vezifeler_DepartamentId",
                table: "Vezifeler",
                column: "DepartamentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "Avanslar");

            migrationBuilder.DropTable(
                name: "BankHesablari");

            migrationBuilder.DropTable(
                name: "BayramGunleri");

            migrationBuilder.DropTable(
                name: "Bildirisler");

            migrationBuilder.DropTable(
                name: "Budceler");

            migrationBuilder.DropTable(
                name: "ChatMesajlar");

            migrationBuilder.DropTable(
                name: "Davamiyyetler");

            migrationBuilder.DropTable(
                name: "Elanlar");

            migrationBuilder.DropTable(
                name: "EvezediciTesdiqler");

            migrationBuilder.DropTable(
                name: "GorushIshtirakcilar");

            migrationBuilder.DropTable(
                name: "HesabatTapshiriqlari");

            migrationBuilder.DropTable(
                name: "Xatirlatmalar");

            migrationBuilder.DropTable(
                name: "Xercler");

            migrationBuilder.DropTable(
                name: "XestelikOdenisleri");

            migrationBuilder.DropTable(
                name: "Icazeler");

            migrationBuilder.DropTable(
                name: "IsciAyliqQazanclar");

            migrationBuilder.DropTable(
                name: "IsciGuzestler");

            migrationBuilder.DropTable(
                name: "IsciHYSler");

            migrationBuilder.DropTable(
                name: "IsciMaasTarixceleri");

            migrationBuilder.DropTable(
                name: "IsciMaliyeleri");

            migrationBuilder.DropTable(
                name: "IsciStrukturRollari");

            migrationBuilder.DropTable(
                name: "IsciTeyinatlari");

            migrationBuilder.DropTable(
                name: "KreditMuracietler");

            migrationBuilder.DropTable(
                name: "LoginLogs");

            migrationBuilder.DropTable(
                name: "MaasDetaylari");

            migrationBuilder.DropTable(
                name: "MaasParametrleri");

            migrationBuilder.DropTable(
                name: "Mesajlar");

            migrationBuilder.DropTable(
                name: "MezuniyyetBalanslari");

            migrationBuilder.DropTable(
                name: "OdenisTapsirigiNomreleri");

            migrationBuilder.DropTable(
                name: "OdenisTapsiriqlari");

            migrationBuilder.DropTable(
                name: "PerformansKriteriyalar");

            migrationBuilder.DropTable(
                name: "SenedAccessler");

            migrationBuilder.DropTable(
                name: "senedDovriyyesiIstifadeciIcazeleri");

            migrationBuilder.DropTable(
                name: "SenedFayllar");

            migrationBuilder.DropTable(
                name: "SenedSablonlar");

            migrationBuilder.DropTable(
                name: "SenedTagMaps");

            migrationBuilder.DropTable(
                name: "Sertifikatlar");

            migrationBuilder.DropTable(
                name: "TapshiriqSherhler");

            migrationBuilder.DropTable(
                name: "TelimIshtiraklar");

            migrationBuilder.DropTable(
                name: "UserDepartments");

            migrationBuilder.DropTable(
                name: "UserPermissions");

            migrationBuilder.DropTable(
                name: "VergiPilleleri");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "Mezuniyyetler");

            migrationBuilder.DropTable(
                name: "Gorushler");

            migrationBuilder.DropTable(
                name: "HesabatSablonlari");

            migrationBuilder.DropTable(
                name: "XercKateqoriyalari");

            migrationBuilder.DropTable(
                name: "Xestelikler");

            migrationBuilder.DropTable(
                name: "Guzestler");

            migrationBuilder.DropTable(
                name: "Vezifeler");

            migrationBuilder.DropTable(
                name: "MaasNovleri");

            migrationBuilder.DropTable(
                name: "Maaslar");

            migrationBuilder.DropTable(
                name: "Banklar");

            migrationBuilder.DropTable(
                name: "MusteriHesablari");

            migrationBuilder.DropTable(
                name: "PerformansQiymetlendirmeler");

            migrationBuilder.DropTable(
                name: "Senedler");

            migrationBuilder.DropTable(
                name: "Tagler");

            migrationBuilder.DropTable(
                name: "Tapshiriqlar");

            migrationBuilder.DropTable(
                name: "Telimler");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "HesabatKateqoriyalari");

            migrationBuilder.DropTable(
                name: "Musteriler");

            migrationBuilder.DropTable(
                name: "Valyutalar");

            migrationBuilder.DropTable(
                name: "SenedNovleri");

            migrationBuilder.DropTable(
                name: "Isciler");

            migrationBuilder.DropTable(
                name: "Departament");

            migrationBuilder.DropTable(
                name: "AspNetUsers");
        }
    }
}
