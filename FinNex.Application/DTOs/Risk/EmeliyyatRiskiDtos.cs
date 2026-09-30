using FinNex.Domain.Entities.Risk;

namespace FinNex.Application.DTOs.Risk
{
    /// <summary>Siyahı/detal — MB Qərar 04/1, Əlavə 4-ün oxuma tərəfi.
    /// Enum kodları ilə yanaşı görüntü adları da göndərilir ki, view-da
    /// switch/dictionary təkrarlanmasın (mənbə: <c>EmeliyyatRiskiAdlari</c>).</summary>
    public class EmeliyyatRiskiHadisesiDto : BaseDto
    {
        public string QeydiyyatKodu { get; set; } = "";
        public string StrukturBolme { get; set; } = "";
        public string? MelumatiVerenSexs { get; set; }

        public DateTime HadiseninBasVerdiyiTarix { get; set; }
        public DateTime HadiseninMueyyenlesdirilmeTarixi { get; set; }
        public int? MueyyenlesdirenIsciId { get; set; }
        public string? MueyyenlesdirenIsciAdi { get; set; }

        public string Tesvir { get; set; } = "";
        public string Sebeb { get; set; } = "";

        public RiskDerecesi TezlikDerecesi { get; set; }
        public RiskDerecesi TesirDerecesi { get; set; }

        public BiznesSahesi BiznesSahesi { get; set; }
        public string? BankMehsulu { get; set; }
        public RiskKateqoriyasi1 RiskKateqoriyasi1 { get; set; }
        public RiskKateqoriyasi2 RiskKateqoriyasi2 { get; set; }
        public string? RiskHadisesiNumune { get; set; }

        public ZererTesirKateqoriyasi ZererTesirKateqoriyasi { get; set; }
        public decimal UmumiZererMebleg { get; set; }
        public decimal? PotensialZererMebleg { get; set; }
        public DateTime? BerpaTarixi { get; set; }
        public decimal? BerpaOlunanMebleg { get; set; }
        public decimal? SigortaIleBerpaOlunanHisse { get; set; }

        public DateTime? TedbirlerinTarixi { get; set; }
        public string? TedbirlerinTesviri { get; set; }
        public string? TedbirlereMesulBolme { get; set; }
        public TedbirIcraStatusu? TedbirlerinIcraStatusu { get; set; }

        public string? SonDeyisiklikTesviri { get; set; }
        public DateTime? YenilenmeTarixi { get; set; }

        public int? YaradanIcraciId { get; set; }
        public string? YaradanIsciAdi { get; set; }

        /// <summary>Ekran/forma üçün: tədbirlər planı (23-26) bu qeyddə
        /// MƏCBURİDİR (ÜmumiZərər &gt;= bankın öz həddi) — servis hesablayır,
        /// view yalnız oxuyur.</summary>
        public bool TedbirlerMecburidir { get; set; }
    }

    /// <summary>Yaratma — istifadəçi (Risk/AML işçisi) hadisəni müşahidə edən
    /// kimi doldurur, təsdiq addımı YOXDUR (30.09.2026 qərarı).</summary>
    public class EmeliyyatRiskiHadisesiCreateDto
    {
        public string StrukturBolme { get; set; } = "";
        public string? MelumatiVerenSexs { get; set; }

        public DateTime HadiseninBasVerdiyiTarix { get; set; }
        public DateTime HadiseninMueyyenlesdirilmeTarixi { get; set; }
        public int? MueyyenlesdirenIsciId { get; set; }

        public string Tesvir { get; set; } = "";
        public string Sebeb { get; set; } = "";

        public RiskDerecesi TezlikDerecesi { get; set; }
        public RiskDerecesi TesirDerecesi { get; set; }

        public BiznesSahesi BiznesSahesi { get; set; }
        public string? BankMehsulu { get; set; }
        public RiskKateqoriyasi1 RiskKateqoriyasi1 { get; set; }
        public RiskKateqoriyasi2 RiskKateqoriyasi2 { get; set; }
        public string? RiskHadisesiNumune { get; set; }

        public ZererTesirKateqoriyasi ZererTesirKateqoriyasi { get; set; }
        public decimal UmumiZererMebleg { get; set; }
        public decimal? PotensialZererMebleg { get; set; }
        public DateTime? BerpaTarixi { get; set; }
        public decimal? BerpaOlunanMebleg { get; set; }
        public decimal? SigortaIleBerpaOlunanHisse { get; set; }

        public DateTime? TedbirlerinTarixi { get; set; }
        public string? TedbirlerinTesviri { get; set; }
        public string? TedbirlereMesulBolme { get; set; }
        public TedbirIcraStatusu? TedbirlerinIcraStatusu { get; set; }
    }

    /// <summary>Redaktə — <see cref="DeyisiklikTesviri"/> MƏCBURİDİR, servis
    /// köhnə qeydin tam snapshotunu <see cref="EmeliyyatRiskiHadisesiTarixce"/>-ə
    /// yazandan SONRA yeni dəyərləri tətbiq edir.</summary>
    public class EmeliyyatRiskiHadisesiUpdateDto : EmeliyyatRiskiHadisesiCreateDto
    {
        public int Id { get; set; }
        public string DeyisiklikTesviri { get; set; } = "";
    }

    public class EmeliyyatRiskiHadisesiTarixceDto : BaseDto
    {
        public string DeyisiklikTesviri { get; set; } = "";
        public int? YaradanIcraciId { get; set; }
        public string? DeyisdirenIsciAdi { get; set; }
    }

    public class EmeliyyatRiskiParametrleriDto
    {
        public decimal TedbirZererHeddi { get; set; }
    }
}
