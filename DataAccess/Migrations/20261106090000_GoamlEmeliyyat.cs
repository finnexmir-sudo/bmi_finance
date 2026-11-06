using System;
using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <summary>
    /// GOAML — "Əməliyyatlar" tabı, təsdiqlənmiş hesabat qeydləri (06.11.2026).
    /// Oracle `ODB.GOAML_CLIENTS`-in güzgüsü — bax <see cref="FinNex.Domain.Entities.Goaml.GoamlEmeliyyat"/>
    /// başındakı şərh. `InsertData` İŞLƏDİLMİR (CLAUDE.md qaydası), seed yoxdur.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261106090000_GoamlEmeliyyat")]
    public partial class GoamlEmeliyyat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GoamlEmeliyyatlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),

                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    TesdiqleyenIsciId = table.Column<int>(type: "int", nullable: false),

                    CandidateFilialNumber = table.Column<int>(type: "int", nullable: false),
                    CandidateDateOper = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CandidateRecNum = table.Column<long>(type: "bigint", nullable: false),

                    Debet = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Kredit = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    VipNetDa = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SummaVInval = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    SummaVNacval = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    KodValuti = table.Column<int>(type: "int", nullable: true),
                    KursValuti = table.Column<decimal>(type: "decimal(18,10)", nullable: true),
                    Tip = table.Column<int>(type: "int", nullable: true),
                    ReportCode = table.Column<int>(type: "int", nullable: true),
                    TransactionNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Primechanie = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DateOper = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TransmodeCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TransmodeComment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AmountLocal = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    FromFundsCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ForeignCurrencyCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ForeignCurrencyCodeFrom = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ForeignAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ForeignExchangeRate = table.Column<decimal>(type: "decimal(18,10)", nullable: true),
                    ToFundsCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Action = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Opened = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Business = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RegistracNomer = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Branch = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Iban = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PersonalAccountType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    GelirMenbeyi = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ToFizik = table.Column<bool>(type: "bit", nullable: true),
                    ToYurik = table.Column<bool>(type: "bit", nullable: true),
                    ToPred = table.Column<bool>(type: "bit", nullable: true),
                    PanTo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),

                    // Nümayəndənin (_CON)
                    ConAdi = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ConAtasininAdi = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ConSoyadi = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ConDogulduguTarix = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConDogulduguYer = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ConVetendasligi = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ConTelefonNovu = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ConRabiteNovu = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ConTelefonNomresi = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ConUnvaninNovu = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ConUnvan = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ConSeher = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ConOlke = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ConSenedinNovu = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ConSenedinSeriyaNomresi = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ConSenedinVerilmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConSenedinEtibarliliqMuddeti = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConVerenOrqan = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ConVerildiyiOlke = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ConCinsi = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    ConPin = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ReprIdType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ReprIdSerial = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ReprIdNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),

                    // Hesab imza hüququ olan həxsin (_SIG)
                    SigAdi = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SigAtasininAdi = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SigSoyadi = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SigDogulduguTarix = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SigDogulduguYer = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SigVetendasligi = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SigTelefonNovu = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SigRabiteNovu = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SigTelefonNomresi = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SigUnvaninNovu = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SigUnvan = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SigSeher = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SigOlke = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SigSenedinNovu = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SigSenedinSeriyaNomresi = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SigSenedinVerilmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SigSenedinEtibarliliqMuddeti = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SigVerenOrqan = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SigVerildiyiOlke = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SigVezife = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SigIsYeriAdi = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),

                    // Hüquqi şəxsin (suffikssiz, göndərən tərəf)
                    InstitutionName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Swift = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Account = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IncorporationNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IncorporationCountryCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    NameUr = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    FromCountry = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    HesabinAcilmaTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BankinAdi = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SeherinKodu = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FealiyyatIstiqameti = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),

                    // Alan fiziki həxsin (_TO, fiziki)
                    ToAdi = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ToAtasininAdi = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ToSoyadi = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ToDogulduguTarix = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ToDogulduguYer = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ToVetendasligi = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ToTelefonNovu = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ToRabiteNovu = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ToTelefonNomresi = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ToUnvaninNovu = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ToUnvan = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ToSeher = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ToOlke = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ToOlkeAdi = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ToSenedinNovu = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ToSenedinSeriyaNomresi = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ToSenedinVerilmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ToSenedinEtibarliliqMuddeti = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ToVerenOrqan = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ToVerildiyiOlke = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),

                    // Banka aid olmayan müş. Hüquq şəxsin (_TO, hüquqi)
                    IncorporationCountryCodeTo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IncorporationNumberTo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NameUrTo = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    InstitutionNameTo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SwiftTo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AccountTo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AlaqeNovuTo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),

                    // Dəqiq bloka aid edilməmiş
                    Gender = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Snn = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    GenderTo = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    SnnTo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Occupation = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    EmployerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SenderIdType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SenderIdSerial = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SenderIdNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RecIdType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RecIdSerial = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RecIdNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),

                    YaradilmaTarixi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    YaradanIcraciId = table.Column<int>(type: "int", nullable: true),
                    YenileyenIcraciId = table.Column<int>(type: "int", nullable: true),
                    SilenIcraciId = table.Column<int>(type: "int", nullable: true),
                    YenilenmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Silinib = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    SilinmeTarixi = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoamlEmeliyyatlar", x => x.Id);
                });

            // Eyni namizəd əməliyyat ikinci dəfə "Təsdiqlə" edilməsin (operatorun
            // səhvən iki dəfə klikləməsi, ya da fərqli axtarışlarda eyni sətrin
            // seçilib yenidən göndərilməsi) — tarixi 3 hissəli açarla.
            migrationBuilder.CreateIndex(
                name: "IX_GoamlEmeliyyatlar_Candidate",
                table: "GoamlEmeliyyatlar",
                columns: new[] { "CandidateFilialNumber", "CandidateDateOper", "CandidateRecNum" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "GoamlEmeliyyatlar");
        }
    }
}
