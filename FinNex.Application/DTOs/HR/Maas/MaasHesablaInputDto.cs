using FinNex.Domain.Entities.HR;

namespace FinNex.Application.DTOs.HR.Maas
{
    // ── TOPLU INPUT ───────────────────────────────────────────────
    public class TopluHesablaInputDto
    {
        public int Il { get; set; }
        public int Ay { get; set; }
        public List<FerdiElaveDto> FerdiElaveler { get; set; } = new();
    }

    // ── NƏTİCƏ DTO-LAR ───────────────────────────────────────────

    public class MaasHesablaNeticesiDto
    {
        public int MaasId { get; set; }
        public int IsciId { get; set; }
        public string IsciAdSoyad { get; set; } = null!;
        public string DepartamentAd { get; set; } = null!;
        public string VezifeAd { get; set; } = null!;
        public int Il { get; set; }
        public int Ay { get; set; }
        public decimal EsasMaas { get; set; }
        public decimal BonusMeblegi { get; set; }
        public int QayibGunSayi { get; set; }
        public decimal QayibKesintisi { get; set; }
        public int MezuniyyetGunSayi { get; set; }
        public decimal MezuniyyetOdenisi { get; set; }
        public decimal MezuniyyetEsasMaasKesintisi { get; set; }
        public decimal CerimeMeblegi { get; set; }
        public decimal BrutMaas { get; set; }
        public decimal VergiGuzesti { get; set; }
        public decimal GelirVergisi { get; set; }
        public decimal DsmfIsci { get; set; }
        public decimal IssizlikIsci { get; set; }
        public decimal Itss { get; set; }
        public decimal UmumiTutulma => GelirVergisi + DsmfIsci + IssizlikIsci + Itss;
        public decimal NetMaas { get; set; }
        public decimal DsmfIsegoturen { get; set; }
        public decimal IssizlikIsegoturen { get; set; }
        public decimal UmumiSirketXerci { get; set; }
        public List<HesablamaIzahiDto> Izahatlar { get; set; } = new();
    }

    public class HesablamaIzahiDto
    {
        public string Addim { get; set; } = null!;
        public string Izah { get; set; } = null!;
        public decimal Mebleg { get; set; }
        public string Tip { get; set; } = null!; // gelir, kesinti, vergi, melumati, sirket
    }

    public class TopluHesablamaNeticesiDto
    {
        public int Il { get; set; }
        public int Ay { get; set; }
        public int UgurluSayi { get; set; }
        public int XetaliSayi { get; set; }
        public int AtlananSayi { get; set; }
        public decimal UmumiNetMebleg { get; set; }
        public List<string> Xetalar { get; set; } = new();
    }

    // ── SİYAHI ───────────────────────────────────────────────────
    public class MaasListDto
    {
        public int Id { get; set; }
        public int IsciId { get; set; }
        public string IsciAdSoyad { get; set; } = null!;
        public string DepartamentAd { get; set; } = null!;
        public string VezifeAd { get; set; } = null!;
        public string? BankHesabNo { get; set; }
        public int Il { get; set; }
        public int Ay { get; set; }
        public decimal EsasMaas { get; set; }
        public decimal BonusMeblegi { get; set; }
        public decimal MezuniyyetOdenisi { get; set; }
        public decimal MezuniyyetEsasMaasKesintisi { get; set; }
        public decimal CerimeMeblegi { get; set; }
        public decimal BrutMaas { get; set; }
        public decimal GelirVergisi { get; set; }
        public decimal DsmfIsci { get; set; }
        public decimal IssizlikIsci { get; set; }
        public decimal Itss { get; set; }
        public decimal NetMebleg { get; set; }
        public MaasStatus Status { get; set; }
        public DateTime HesablanmaTarixi { get; set; }
        public DateTime? TesdiqTarixi { get; set; }
        public DateTime? OdenisTarixi { get; set; }
    }

    // ── DETAL ────────────────────────────────────────────────────
    public class MaasDto
    {
        public int Id { get; set; }
        public int IsciId { get; set; }
        public string IsciAdSoyad { get; set; } = null!;
        public string DepartamentAd { get; set; } = null!;
        public string VezifeAd { get; set; } = null!;
        public string? BankHesabNo { get; set; }
        public int Il { get; set; }
        public int Ay { get; set; }
        public decimal NetMebleg { get; set; }
        public MaasStatus Status { get; set; }
        public DateTime HesablanmaTarixi { get; set; }
        public DateTime? TesdiqTarixi { get; set; }
        public DateTime? OdenisTarixi { get; set; }
        public List<MaasDetayDto> Detallar { get; set; } = new();
        public List<HesablamaIzahiDto> Izahatlar { get; set; } = new();
    }

    public class MaasDetayDto
    {
        public int Id { get; set; }
        public string MaasNovuAd { get; set; } = null!;
        public MaasDetayTipi Tip { get; set; }
        public decimal Mebleg { get; set; }
        public string? Aciqlama { get; set; }
    }

    // ── CRUD ─────────────────────────────────────────────────────
    public class MaasCreateDto
    {
        public int IsciId { get; set; }
        public int Il { get; set; }
        public int Ay { get; set; }
    }

    public class MaasUpdateDto
    {
        public int Id { get; set; }
        public MaasStatus Status { get; set; }
    }

    // ── VERGİ PARAMETRLƏRİ (daxili) ──────────────────────────────
    public class VergiParametrlerDto
    {
        public decimal GelirVergisiFaizi { get; set; } = 14m;
        public decimal DsmfFaizi { get; set; } = 3m;
        public decimal IssizlikSigortasiFaizi { get; set; } = 0.5m;
        public decimal IcbariTibbiSigortaFaizi { get; set; } = 2m;
        public decimal VergiGuzestiMeblegi { get; set; } = 200m;
        public decimal MinimumEmekHaqqi { get; set; } = 345m;
        public decimal DsmfIsegotürenFaizi { get; set; } = 22m;
        public decimal IssizlikIsegotürenFaizi { get; set; } = 0.5m;
    }

    // ── BANK ─────────────────────────────────────────────────────
    public class BankOdenisDto
    {
        public string IsciAdSoyad { get; set; } = null!;
        public string IBAN { get; set; } = null!;
        public decimal NetMebleg { get; set; }
        public int Il { get; set; }
        public int Ay { get; set; }
        public string Izah => $"{Il}/{Ay:D2} əmək haqqı";
    }
}