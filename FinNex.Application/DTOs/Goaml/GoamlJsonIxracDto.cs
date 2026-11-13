using System.Text.Json.Serialization;
using FinNex.Domain.Entities.Goaml;

namespace FinNex.Application.DTOs.Goaml
{
    /// <summary>
    /// GOAML — Export to JSON (#15 tapşırığı, 12.11.2026) sorğu filtri.
    /// `JSON_MMX_IN`-in `(p_date_S, p_date_PO, v_report_code, V_tip)`
    /// parametrlərinin güzgüsüdür — amma bizim versiyamızda `Tip`/`ReportCode`
    /// İSTƏYƏ BAĞLI (null = hamısı) — Oracle-dan FƏRQLİ olaraq tək çağırışda
    /// qarışıq Tip/ReportCode-lu qeydləri BİRLİKDƏ ixrac edə bilir, çünki hər
    /// qeydin header-i (reportType/reportReason) ÖZ ReportCode-undan AYRI-AYRI
    /// hesablanır (bax <see cref="GoamlService.JsonIxracAsync"/>).
    /// </summary>
    public class GoamlJsonIxracFiltrDto
    {
        public DateTime BasTarix { get; set; }
        public DateTime SonTarix { get; set; }
        public GoamlTipi? Tip { get; set; }
        public GoamlHesabatNovu? ReportCode { get; set; }
    }

    /// <summary>İxrac nəticəsi — JSON mətni servis qatında hazırlanır, fayla
    /// yazmaq/DMS-ə qoymaq İSƏ controller-in işidir (Exchange/Kassa modulunun
    /// `SablonYolu()` presedentinə uyğun — DMS yolu HƏMİŞƏ controller-dədir).</summary>
    public class GoamlJsonIxracNeticeDto
    {
        public string JsonMetni { get; set; } = "";
        public string TeklifOlunanFaylAdi { get; set; } = "";
        public int SetirSayi { get; set; }
        /// <summary>Oracle-dan (malumatin_novu/kurval) lazımi dəyər tapılmayan
        /// qeydlərin xəbərdarlıqları — ixracı DAYANDIRMIR, YALNIZ operatora
        /// göstərilir (CLAUDE.md-nin "best-effort, Oracle əlçatmaz olsa belə
        /// sınmasın" qaydasına uyğun).</summary>
        public List<string> Xeberdarliqlar { get; set; } = new();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // goAML JSON sxemi — AYSEL-in `JSON_MMX_IN` prosedurunun (11.11.2026-da
    // arxivlənmiş, docs/sql/goaml/97_Goaml_JSON_MMX_IN_Tam_Metn.sql) UTL_FILE
    // ilə yazdığı mətnin BİRƏ-BİR struktur güzgüsü. Sahə adları (header=camelCase,
    // blok açarları="Transaction"/"Sender" kimi PascalCase, leaf sahələr=
    // trn_status kimi snake_case) Oracle-ın ÖZ qarışıq konvensiyası ilə EYNİ
    // SAXLANILIB — bu, bizim seçimimiz deyil, goAML-in XARİCİ sxemidir,
    // dəyişdirilə bilməz. Hər sahə `[JsonPropertyName]` ilə AÇIQ işarələnib,
    // System.Text.Json-un default naming policy-sinə GÜVƏNİLMİR (sxem qarışıq
    // olduğu üçün tək bir siyasət hamısına uyğun gəlməz).
    // ═══════════════════════════════════════════════════════════════════════

    public class GoamlJsonRootDto
    {
        [JsonPropertyName("reports")]
        public List<GoamlJsonReportDto> Reports { get; set; } = new();
    }

    public class GoamlJsonReportDto
    {
        [JsonPropertyName("header")]
        public GoamlJsonHeaderDto Header { get; set; } = new();

        [JsonPropertyName("transactions")]
        public List<GoamlJsonTransactionBlocDto> Transactions { get; set; } = new();
    }

    public class GoamlJsonHeaderDto
    {
        [JsonPropertyName("sendDate")]
        public string SendDate { get; set; } = "";

        [JsonPropertyName("reportType")]
        public int ReportType { get; set; }

        [JsonPropertyName("reportReason")]
        public int ReportReason { get; set; }

        // ═══════════════════════════════════════════════════════════════════
        // Header-zənginləşdirməsi — İKİNCİ, DÜZƏLDİLMİŞ VERSİYA (13.11.2026,
        // istifadəçinin göndərdiyi 6 REAL goAML portal form-sxemi — CTR, ETR,
        // BCR, HRZ, NGR, PEP — əsasında).
        //
        // ⚠️ KÖK SƏBƏB TAPILDI: bu sahələr əvvəl AYSEL-in köhnə `JSON_MMX_IN`
        // SQL-indəki DAXİLİ `report_code` (bizim 16-dəyərli `GoamlHesabatNovu`
        // kataloqu — AAR/BLR/CTR/QHT/STR/UNR və s., `malumatin_novu` cədvəlinin
        // AÇARI) yoxlamasına görə qurulmuşdu. REAL goAML portalı isə yalnız
        // `reportType`-i (1=CTR,2=ETR,3=NGR,4=PEP,6=SAR,7=TFR,8=HRZ,9=BCR)
        // tanıyır — bizim 16 daxili kod `malumatin_novu` vasitəsilə BU 8
        // dəyərdən birinə TƏRCÜMƏ OLUNUR (`GoamlService.MalumatinNovuTapAsync`).
        // Branch indi bizim daxili kod ADINA (`reportCodeKod == "PEP"`) YOX,
        // Oracle-un qaytardığı HƏMİN `reportType`-ə görə seçilir (bax
        // `HeaderZenginlesdirAsync`) — əks halda, məsələn, daxili "STR" kodu
        // (çox güman SAR=6-ya tərcümə olunur) heç vaxt enrichment ALMAZDI.
        //
        // Real sxemlərdən TƏSDİQLƏNDİ: "risk.json" (əvvəl "BLR" sayılan
        // nümunə) əslində reportType=8=**HRZ**-dir, "PEP" (reportType=4) isə
        // TAM FƏRQLİ, bir qədər DAR bir sahə dəstidir — HƏR İKİSİ BİRLİKDƏ
        // paylaşır: citizenship/identityType/serial/documentNumber/fin/
        // customerRiskDegree/country/residency/city/district/street/home/
        // flat/sofIncome/averageMonthlyIncome/additionalSofIncomes/amount/
        // position/workplace/businessStartDate/businessEndDate/note/fullName.
        // YALNIZ PEP-də: pepCountry, positionStartDate/EndDate,
        // typeOfConnectionToPep, politicallyInfluentialPersonPositions,
        // institution. YALNIZ HRZ-də: individualPerson, tin,
        // countryOfRegistration, sphereOfActivity, typeOfConnectionToHrz,
        // hrzCountry, idNumber. ("influentialPerson" adlı sahə HEÇ BİR real
        // sxemdə YOXDUR — əvvəlki versiyada səhvən əlavə edilmişdi, silindi.)
        //
        // NGR (reportType=3) TAMAMİLƏ YENİ, SADƏ bir sxemdir —
        // tin/tinDate/organizationType(QHT və ya Dini təşkilat)/note. ⚠️ "QHT"
        // AYRICA hesabat növü DEYİL — NGR-in daxilindəki `organizationType`
        // seçimlərindən BİRİDİR (əvvəlki "registrac_nomer_k tapılmadı, QHT
        // port edilmədi" qeydi bu səbəbdən TAMAMİLƏ YANLIŞ İZ İMİŞ — QHT-nin
        // `v_PV_HUQUQI`/`huquqi_shexs` ilə heç bir əlaqəsi yoxdur). `tinDate`/
        // `organizationType` üçün Oracle-da mənbə HƏLƏ TAPILMAYIB (97_...sql-
        // də NGR-ə aid qol YOXDUR, bu, köhnə SQL-dən DEYİL, yeni real
        // nümunədən gəlir) — operator bunları ƏL İLƏ doldurmalıdır, YALNIZ
        // `tin` (VÖEN) digər budaqlarla EYNİ Oracle mənbəyindən (`InnRegnom`)
        // avtomatik doldurulur.
        //
        // ⚠️ SAR (6) və TFR (7) HƏLƏ GÖRÜLMƏYİB — real nümunə gələnə qədər
        // bu iki `reportType` üçün HEÇ BİR enrichment yazılmayıb (header
        // yalnız sendDate/reportType/reportReason daşıyır), uydurma sahə
        // ƏLAVƏ EDİLMƏYİB.
        //
        // Hamısı NULLABLE: `JsonIxracAsync`-in serializer-i `WhenWritingNull`
        // ilə qurulub — reportType 3/4/8 olmayanda (CTR/ETR/BCR/SAR/TFR) bu
        // sahələrin HEÇ BİRİ JSON-a yazılmır.
        // ═══════════════════════════════════════════════════════════════════

        [JsonPropertyName("individualPerson")]
        public bool? IndividualPerson { get; set; }

        [JsonPropertyName("tin")]
        public string? Tin { get; set; }

        [JsonPropertyName("countryOfRegistration")]
        public string? CountryOfRegistration { get; set; }

        [JsonPropertyName("citizenship")]
        public string? Citizenship { get; set; }

        [JsonPropertyName("identityType")]
        public int? IdentityType { get; set; }

        [JsonPropertyName("serial")]
        public string? Serial { get; set; }

        [JsonPropertyName("documentNumber")]
        public string? DocumentNumber { get; set; }

        [JsonPropertyName("fin")]
        public string? Fin { get; set; }

        [JsonPropertyName("sphereOfActivity")]
        public string? SphereOfActivity { get; set; }

        [JsonPropertyName("customerRiskDegree")]
        public int? CustomerRiskDegree { get; set; }

        [JsonPropertyName("pepCountry")]
        public string? PepCountry { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }

        [JsonPropertyName("residency")]
        public string? Residency { get; set; }

        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("district")]
        public string? District { get; set; }

        [JsonPropertyName("street")]
        public string? Street { get; set; }

        [JsonPropertyName("home")]
        public string? Home { get; set; }

        [JsonPropertyName("flat")]
        public string? Flat { get; set; }

        [JsonPropertyName("sofIncome")]
        public decimal? SofIncome { get; set; }

        [JsonPropertyName("averageMonthlyIncome")]
        public decimal? AverageMonthlyIncome { get; set; }

        [JsonPropertyName("position")]
        public string? Position { get; set; }

        [JsonPropertyName("positionStartDate")]
        public string? PositionStartDate { get; set; }

        [JsonPropertyName("positionEndDate")]
        public string? PositionEndDate { get; set; }

        [JsonPropertyName("businessStartDate")]
        public string? BusinessStartDate { get; set; }

        [JsonPropertyName("businessEndDate")]
        public string? BusinessEndDate { get; set; }

        [JsonPropertyName("note")]
        public string? Note { get; set; }

        [JsonPropertyName("workplace")]
        public string? Workplace { get; set; }

        [JsonPropertyName("typeOfConnectionToPep")]
        public int? TypeOfConnectionToPep { get; set; }

        [JsonPropertyName("fullName")]
        public string? FullName { get; set; }

        [JsonPropertyName("institution")]
        public string? Institution { get; set; }

        [JsonPropertyName("politicallyInfluentialPersonPositions")]
        public List<int>? PoliticallyInfluentialPersonPositions { get; set; }

        [JsonPropertyName("typeOfConnectionToHrz")]
        public int? TypeOfConnectionToHrz { get; set; }

        [JsonPropertyName("hrzCountry")]
        public string? HrzCountry { get; set; }

        [JsonPropertyName("idNumber")]
        public string? IdNumber { get; set; }

        // PEP (4) VƏ HRZ (8) — HƏR İKİSİNDƏ var, real sxemdən təsdiqləndi
        // (13.11.2026). Oracle-da mənbə tapılmayıb — operator əl ilə doldurur,
        // boş qalanda "additionalSofIncomes doldurulub, amount tələb olunur"
        // asılılığı pozulmur (ikisi də boşdur, dependency işə düşmür).
        [JsonPropertyName("amount")]
        public decimal? Amount { get; set; }

        [JsonPropertyName("additionalSofIncomes")]
        public List<int>? AdditionalSofIncomes { get; set; }

        // NGR (reportType=3) — YENİ, 13.11.2026-da real nümunədən tapıldı.
        [JsonPropertyName("tinDate")]
        public string? TinDate { get; set; }

        [JsonPropertyName("organizationType")]
        public int? OrganizationType { get; set; }
    }

    public class GoamlJsonTransactionBlocDto
    {
        [JsonPropertyName("Transaction")]
        public GoamlJsonTransactionDto Transaction { get; set; } = new();

        [JsonPropertyName("Representative")]
        public GoamlJsonRepresentativeDto Representative { get; set; } = new();

        [JsonPropertyName("Sender")]
        public GoamlJsonSenderDto Sender { get; set; } = new();

        [JsonPropertyName("Receiver")]
        public GoamlJsonReceiverDto Receiver { get; set; } = new();

        [JsonPropertyName("Correspondence")]
        public GoamlJsonCorrespondenceDto Correspondence { get; set; } = new();
    }

    public class GoamlJsonTransactionDto
    {
        [JsonPropertyName("trn_status")]
        public string TrnStatus { get; set; } = "EXC";

        [JsonPropertyName("trn_actual_date")]
        public string TrnActualDate { get; set; } = "";

        [JsonPropertyName("trn_bank_date")]
        public string TrnBankDate { get; set; } = "";

        [JsonPropertyName("trn_external_number")]
        public string TrnExternalNumber { get; set; } = "";

        [JsonPropertyName("trn_internal_number")]
        public string TrnInternalNumber { get; set; } = "";

        [JsonPropertyName("trn_type")]
        public string TrnType { get; set; } = "";

        [JsonPropertyName("trn_payment_system")]
        public string TrnPaymentSystem { get; set; } = "";

        [JsonPropertyName("trn_payment_sub_system")]
        public string TrnPaymentSubSystem { get; set; } = "";

        [JsonPropertyName("Purpose")]
        public GoamlJsonPurposeDto Purpose { get; set; } = new();

        [JsonPropertyName("trn_chanel")]
        public string TrnChanel { get; set; } = "";
    }

    public class GoamlJsonPurposeDto
    {
        [JsonPropertyName("trn_description")]
        public string TrnDescription { get; set; } = "";

        [JsonPropertyName("trn_detailed_id")]
        public string TrnDetailedId { get; set; } = "";

        [JsonPropertyName("trn_fund_source")]
        public string TrnFundSource { get; set; } = "";
    }

    public class GoamlJsonRepresentativeDto
    {
        [JsonPropertyName("repr_status")]
        public string ReprStatus { get; set; } = "NRP";

        [JsonPropertyName("repr_citizenship")]
        public string? ReprCitizenship { get; set; }

        [JsonPropertyName("repr_id_serial")]
        public string? ReprIdSerial { get; set; }

        [JsonPropertyName("repr_id_number")]
        public string? ReprIdNumber { get; set; }

        [JsonPropertyName("repr_id_issue_date")]
        public string? ReprIdIssueDate { get; set; }

        [JsonPropertyName("repr_id_expiry_date")]
        public string? ReprIdExpiryDate { get; set; }

        [JsonPropertyName("repr_birth_date")]
        public string? ReprBirthDate { get; set; }

        [JsonPropertyName("repr_phone")]
        public string? ReprPhone { get; set; }

        [JsonPropertyName("repr_pin")]
        public string? ReprPin { get; set; }

        [JsonPropertyName("repr_first_name")]
        public string? ReprFirstName { get; set; }

        [JsonPropertyName("repr_last_name")]
        public string? ReprLastName { get; set; }

        [JsonPropertyName("repr_middle_name")]
        public string? ReprMiddleName { get; set; }

        [JsonPropertyName("repr_id_type")]
        public string? ReprIdType { get; set; }
    }

    // ── Sender ───────────────────────────────────────────────────────────
    public class GoamlJsonSenderDto
    {
        [JsonPropertyName("sender_country_name")]
        public string SenderCountryName { get; set; } = "";

        [JsonPropertyName("Entity")]
        public GoamlJsonSenderEntityDto? Entity { get; set; }

        [JsonPropertyName("Person")]
        public GoamlJsonSenderPersonDto? Person { get; set; }

        [JsonPropertyName("Funds")]
        public GoamlJsonSenderFundsDto Funds { get; set; } = new();

        [JsonPropertyName("Account")]
        public GoamlJsonSenderAccountDto? Account { get; set; }
    }

    public class GoamlJsonSenderEntityDto
    {
        [JsonPropertyName("sender_entity_name")]
        public string SenderEntityName { get; set; } = "";

        [JsonPropertyName("sender_entity_reg_country")]
        public string SenderEntityRegCountry { get; set; } = "";

        [JsonPropertyName("sender_entity_tax_number")]
        public string SenderEntityTaxNumber { get; set; } = "";
    }

    public class GoamlJsonSenderPersonDto
    {
        [JsonPropertyName("sender_first_name")]
        public string SenderFirstName { get; set; } = "";

        [JsonPropertyName("sender_last_name")]
        public string SenderLastName { get; set; } = "";

        [JsonPropertyName("sender_middle_name")]
        public string SenderMiddleName { get; set; } = "";

        [JsonPropertyName("sender_citizenship")]
        public string SenderCitizenship { get; set; } = "";

        [JsonPropertyName("sender_id_serial")]
        public string SenderIdSerial { get; set; } = "";

        [JsonPropertyName("sender_id_number")]
        public string SenderIdNumber { get; set; } = "";

        [JsonPropertyName("sender_pin_code")]
        public string SenderPinCode { get; set; } = "";

        [JsonPropertyName("sender_id_issue_date")]
        public string SenderIdIssueDate { get; set; } = "";

        [JsonPropertyName("sender_id_expiry_date")]
        public string SenderIdExpiryDate { get; set; } = "";

        [JsonPropertyName("sender_birth_date")]
        public string SenderBirthDate { get; set; } = "";

        [JsonPropertyName("sender_phone_number")]
        public string SenderPhoneNumber { get; set; } = "";

        [JsonPropertyName("sender_tax_id")]
        public string SenderTaxId { get; set; } = "";

        [JsonPropertyName("sender_id_type")]
        public string SenderIdType { get; set; } = "";
    }

    public class GoamlJsonSenderFundsDto
    {
        [JsonPropertyName("sender_curr_type")]
        public string SenderCurrType { get; set; } = "";

        [JsonPropertyName("sender_curr_amount")]
        public decimal SenderCurrAmount { get; set; }

        [JsonPropertyName("sender_amount_local")]
        public decimal SenderAmountLocal { get; set; }
    }

    public class GoamlJsonSenderAccountDto
    {
        [JsonPropertyName("sender_account_number")]
        public string SenderAccountNumber { get; set; } = "";

        [JsonPropertyName("sender_account_dubl_number")]
        public string SenderAccountDublNumber { get; set; } = "";

        [JsonPropertyName("sender_account_currency")]
        public string SenderAccountCurrency { get; set; } = "";

        [JsonPropertyName("sender_account_pan_id_number")]
        public string SenderAccountPanIdNumber { get; set; } = "";

        [JsonPropertyName("sender_account_3d_secure")]
        public string SenderAccount3dSecure { get; set; } = "";

        [JsonPropertyName("sender_account_fin_inst_country")]
        public string SenderAccountFinInstCountry { get; set; } = "";

        [JsonPropertyName("sender_account_fin_inst_bic_id")]
        public string SenderAccountFinInstBicId { get; set; } = "";

        [JsonPropertyName("sender_account_fin_inst_name")]
        public string SenderAccountFinInstName { get; set; } = "";

        [JsonPropertyName("sender_account_fin_inst_branch")]
        public string SenderAccountFinInstBranch { get; set; } = "";

        [JsonPropertyName("sender_account_fin_inst_bic")]
        public string SenderAccountFinInstBic { get; set; } = "";

        [JsonPropertyName("sender_account_fin_inst_tax_id")]
        public string SenderAccountFinInstTaxId { get; set; } = "";
    }

    // ── Receiver (Sender-in GÜZGÜSÜ, receiver_* prefiksli) ─────────────────
    public class GoamlJsonReceiverDto
    {
        [JsonPropertyName("receiver_country_name")]
        public string ReceiverCountryName { get; set; } = "";

        [JsonPropertyName("Entity")]
        public GoamlJsonReceiverEntityDto? Entity { get; set; }

        [JsonPropertyName("Person")]
        public GoamlJsonReceiverPersonDto? Person { get; set; }

        [JsonPropertyName("Funds")]
        public GoamlJsonReceiverFundsDto Funds { get; set; } = new();

        [JsonPropertyName("Account")]
        public GoamlJsonReceiverAccountDto? Account { get; set; }
    }

    public class GoamlJsonReceiverEntityDto
    {
        [JsonPropertyName("receiver_entity_name")]
        public string ReceiverEntityName { get; set; } = "";

        [JsonPropertyName("receiver_entity_reg_country")]
        public string ReceiverEntityRegCountry { get; set; } = "";

        [JsonPropertyName("receiver_entity_tax_number")]
        public string ReceiverEntityTaxNumber { get; set; } = "";
    }

    public class GoamlJsonReceiverPersonDto
    {
        [JsonPropertyName("receiver_first_name")]
        public string ReceiverFirstName { get; set; } = "";

        [JsonPropertyName("receiver_last_name")]
        public string ReceiverLastName { get; set; } = "";

        [JsonPropertyName("receiver_middle_name")]
        public string ReceiverMiddleName { get; set; } = "";

        [JsonPropertyName("receiver_citizenship")]
        public string ReceiverCitizenship { get; set; } = "";

        [JsonPropertyName("receiver_id_serial")]
        public string ReceiverIdSerial { get; set; } = "";

        [JsonPropertyName("receiver_id_number")]
        public string ReceiverIdNumber { get; set; } = "";

        [JsonPropertyName("receiver_pin_code")]
        public string ReceiverPinCode { get; set; } = "";

        [JsonPropertyName("receiver_id_issue_date")]
        public string ReceiverIdIssueDate { get; set; } = "";

        [JsonPropertyName("receiver_id_expiry_date")]
        public string ReceiverIdExpiryDate { get; set; } = "";

        [JsonPropertyName("receiver_phone_number")]
        public string ReceiverPhoneNumber { get; set; } = "";

        [JsonPropertyName("receiver_tax_id")]
        public string ReceiverTaxId { get; set; } = "";

        [JsonPropertyName("receiver_birth_date")]
        public string ReceiverBirthDate { get; set; } = "";

        [JsonPropertyName("receiver_id_type")]
        public string ReceiverIdType { get; set; } = "";
    }

    public class GoamlJsonReceiverFundsDto
    {
        [JsonPropertyName("receiver_curr_type")]
        public string ReceiverCurrType { get; set; } = "";

        [JsonPropertyName("receiver_curr_amount")]
        public decimal ReceiverCurrAmount { get; set; }

        [JsonPropertyName("receiver_amount_local")]
        public decimal ReceiverAmountLocal { get; set; }
    }

    public class GoamlJsonReceiverAccountDto
    {
        [JsonPropertyName("receiver_account_number")]
        public string ReceiverAccountNumber { get; set; } = "";

        [JsonPropertyName("receiver_account_dubl_number")]
        public string ReceiverAccountDublNumber { get; set; } = "";

        [JsonPropertyName("receiver_account_currency")]
        public string ReceiverAccountCurrency { get; set; } = "";

        [JsonPropertyName("receiver_account_pan_id_number")]
        public string ReceiverAccountPanIdNumber { get; set; } = "";

        [JsonPropertyName("receiver_account_3dsecure")]
        public string ReceiverAccount3dsecure { get; set; } = "";

        [JsonPropertyName("receiver_account_fin_inst_name")]
        public string ReceiverAccountFinInstName { get; set; } = "";

        [JsonPropertyName("receiver_account_fin_inst_country")]
        public string ReceiverAccountFinInstCountry { get; set; } = "";

        [JsonPropertyName("receiver_account_fin_inst_tax_id")]
        public string ReceiverAccountFinInstTaxId { get; set; } = "";

        [JsonPropertyName("receiver_account_fin_inst_bic_id")]
        public string ReceiverAccountFinInstBicId { get; set; } = "";

        [JsonPropertyName("receiver_account_fin_inst_branch")]
        public string ReceiverAccountFinInstBranch { get; set; } = "";

        [JsonPropertyName("receiver_account_fin_inst_bic")]
        public string ReceiverAccountFinInstBic { get; set; } = "";
    }

    public class GoamlJsonCorrespondenceDto
    {
        [JsonPropertyName("debet")]
        public string Debet { get; set; } = "";

        [JsonPropertyName("Credit")]
        public string Credit { get; set; } = "";
    }
}
