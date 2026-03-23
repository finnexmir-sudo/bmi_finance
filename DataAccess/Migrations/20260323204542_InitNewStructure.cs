using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class InitNewStructure : Migration
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
                name: "MaasNovu",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ad = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Gelirdir = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_MaasNovu", x => x.Id);
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
                name: "Senedler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    SenedNovuId = table.Column<int>(type: "int", nullable: false),
                    Basliq = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AcarSoz = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    VezifeId = table.Column<int>(type: "int", nullable: true),
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
                    table.ForeignKey(
                        name: "FK_Isciler_Vezifeler_VezifeId",
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
                name: "Icazeler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsciId = table.Column<int>(type: "int", nullable: false),
                    EvezEdenIsciId = table.Column<int>(type: "int", nullable: false),
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
                name: "IsciMaasTarixcesi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsciId = table.Column<int>(type: "int", nullable: false),
                    KohneMaas = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    YeniMaas = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DeyismeTarixi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EmrinNomresi = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_IsciMaasTarixcesi", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IsciMaasTarixcesi_Isciler_IsciId",
                        column: x => x.IsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IsciMaliye",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsciId = table.Column<int>(type: "int", nullable: false),
                    CariMaas = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MezuniyyetQaliqGunu = table.Column<int>(type: "int", nullable: false),
                    BankHesabNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SosialSigortaNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_IsciMaliye", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IsciMaliye_Isciler_IsciId",
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
                name: "Maaslar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsciId = table.Column<int>(type: "int", nullable: false),
                    Il = table.Column<int>(type: "int", nullable: false),
                    Ay = table.Column<int>(type: "int", nullable: false),
                    NetMebleg = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsciId1 = table.Column<int>(type: "int", nullable: true),
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
                    table.ForeignKey(
                        name: "FK_Maaslar_Isciler_IsciId1",
                        column: x => x.IsciId1,
                        principalTable: "Isciler",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MezuniyyetBalanslari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsciId = table.Column<int>(type: "int", nullable: false),
                    Il = table.Column<int>(type: "int", nullable: false),
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
                        name: "FK_Mezuniyyetler_Isciler_IsciId",
                        column: x => x.IsciId,
                        principalTable: "Isciler",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MaasDetay",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaasId = table.Column<int>(type: "int", nullable: false),
                    MaasNovuId = table.Column<int>(type: "int", nullable: false),
                    Mebleg = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Acıqlama = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_MaasDetay", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaasDetay_MaasNovu_MaasNovuId",
                        column: x => x.MaasNovuId,
                        principalTable: "MaasNovu",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MaasDetay_Maaslar_MaasId",
                        column: x => x.MaasId,
                        principalTable: "Maaslar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                name: "IX_BankHesablari_BankId",
                table: "BankHesablari",
                column: "BankId");

            migrationBuilder.CreateIndex(
                name: "IX_BankHesablari_ValyutaId",
                table: "BankHesablari",
                column: "ValyutaId");

            migrationBuilder.CreateIndex(
                name: "IX_Davamiyyetler_IsciId_Tarix",
                table: "Davamiyyetler",
                columns: new[] { "IsciId", "Tarix" },
                unique: true);

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
                name: "IX_Isciler_VezifeId",
                table: "Isciler",
                column: "VezifeId");

            migrationBuilder.CreateIndex(
                name: "IX_IsciMaasTarixcesi_IsciId",
                table: "IsciMaasTarixcesi",
                column: "IsciId");

            migrationBuilder.CreateIndex(
                name: "IX_IsciMaliye_IsciId",
                table: "IsciMaliye",
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
                name: "IX_MaasDetay_MaasId",
                table: "MaasDetay",
                column: "MaasId");

            migrationBuilder.CreateIndex(
                name: "IX_MaasDetay_MaasNovuId",
                table: "MaasDetay",
                column: "MaasNovuId");

            migrationBuilder.CreateIndex(
                name: "IX_Maaslar_IsciId_Il_Ay",
                table: "Maaslar",
                columns: new[] { "IsciId", "Il", "Ay" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Maaslar_IsciId1",
                table: "Maaslar",
                column: "IsciId1");

            migrationBuilder.CreateIndex(
                name: "IX_MezuniyyetBalanslari_IsciId",
                table: "MezuniyyetBalanslari",
                column: "IsciId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MezuniyyetBalanslari_IsciId_Il",
                table: "MezuniyyetBalanslari",
                columns: new[] { "IsciId", "Il" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Mezuniyyetler_EvezEdenIsciId",
                table: "Mezuniyyetler",
                column: "EvezEdenIsciId");

            migrationBuilder.CreateIndex(
                name: "IX_Mezuniyyetler_IsciId",
                table: "Mezuniyyetler",
                column: "IsciId");

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
                name: "IX_SenedAccessler_SenedId",
                table: "SenedAccessler",
                column: "SenedId");

            migrationBuilder.CreateIndex(
                name: "IX_SenedAccessler_SenedId1",
                table: "SenedAccessler",
                column: "SenedId1");

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
                name: "IX_SenedTagMaps_TagId",
                table: "SenedTagMaps",
                column: "TagId");

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
                name: "IX_Vezifeler_Ad",
                table: "Vezifeler",
                column: "Ad",
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
                name: "BankHesablari");

            migrationBuilder.DropTable(
                name: "BayramGunleri");

            migrationBuilder.DropTable(
                name: "Davamiyyetler");

            migrationBuilder.DropTable(
                name: "Icazeler");

            migrationBuilder.DropTable(
                name: "IsciMaasTarixcesi");

            migrationBuilder.DropTable(
                name: "IsciMaliye");

            migrationBuilder.DropTable(
                name: "IsciStrukturRollari");

            migrationBuilder.DropTable(
                name: "IsciTeyinatlari");

            migrationBuilder.DropTable(
                name: "MaasDetay");

            migrationBuilder.DropTable(
                name: "MezuniyyetBalanslari");

            migrationBuilder.DropTable(
                name: "Mezuniyyetler");

            migrationBuilder.DropTable(
                name: "OdenisTapsirigiNomreleri");

            migrationBuilder.DropTable(
                name: "OdenisTapsiriqlari");

            migrationBuilder.DropTable(
                name: "SenedAccessler");

            migrationBuilder.DropTable(
                name: "SenedFayllar");

            migrationBuilder.DropTable(
                name: "SenedTagMaps");

            migrationBuilder.DropTable(
                name: "UserDepartments");

            migrationBuilder.DropTable(
                name: "UserPermissions");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "MaasNovu");

            migrationBuilder.DropTable(
                name: "Maaslar");

            migrationBuilder.DropTable(
                name: "Banklar");

            migrationBuilder.DropTable(
                name: "MusteriHesablari");

            migrationBuilder.DropTable(
                name: "Senedler");

            migrationBuilder.DropTable(
                name: "Tagler");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "Isciler");

            migrationBuilder.DropTable(
                name: "Musteriler");

            migrationBuilder.DropTable(
                name: "Valyutalar");

            migrationBuilder.DropTable(
                name: "SenedNovleri");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Vezifeler");

            migrationBuilder.DropTable(
                name: "Departament");
        }
    }
}
