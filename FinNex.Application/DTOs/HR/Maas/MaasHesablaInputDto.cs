using FinNex.Domain.Entities.HR;

namespace FinNex.Application.DTOs.HR.Maas
{
    // ── İŞÇİ AYLIQ QAZANC (məzuniyyət üçün 12 ay) ─────────────────
    public class IsciAyliqQazancDto
    {
        public int Id { get; set; }
        public int IsciId { get; set; }
        public int Il { get; set; }
        public int Ay { get; set; }
        public decimal Qazanc { get; set; }
        public bool ElIleDaxilEdilib { get; set; }
        public string? Qeyd { get; set; }
        public DateTime YaradilmaTarixi { get; set; }
    }

    // ── XƏSTƏLİK ─────────────────────────────────────────────────
    public class XestelikDto
    {
        public int Id { get; set; }
        public int IsciId { get; set; }
        public string IsciAdSoyad { get; set; } = "";
        public DateTime BaslamaTarixi { get; set; }
        public DateTime BitmeTarixi { get; set; }
        public int IsGunSayi { get; set; }
        public string BulletenNomresi { get; set; } = "";
        public string? MualiceMuessisesi { get; set; }
        public string? Qeyd { get; set; }
        public int Status { get; set; }
        public DateTime? HrTesdiqTarixi { get; set; }
        public decimal? UmumiSirketOdenisi { get; set; }
    }

    public class XestelikCreateDto
    {
        public int IsciId { get; set; }
        public DateTime BaslamaTarixi { get; set; }
        public DateTime BitmeTarixi { get; set; }
        public string BulletenNomresi { get; set; } = "";
        public string? MualiceMuessisesi { get; set; }
        public string? Qeyd { get; set; }
    }

    public class XestelikPreviewDto
    {
        public int IsGunSayi { get; set; }

        // ── Gəlir (S) ──────────────────────────────────────────
        public decimal S { get; set; }                         // Son 12 ay cəmi qazanc (ham)
        public decimal Son12XestelikOdenisi { get; set; }      // Bu müddətdə alınmış xəstəlik (çıxılır)
        public decimal SEffektiv => S - Son12XestelikOdenisi;  // Formulada istifadə olunan gəlir

        // ── İş günləri ─────────────────────────────────────────
        public int Son12AyIsGunu { get; set; }                 // Son 12 ay iş günü (ham)
        public int Son12AyXestelikGunu { get; set; }           // Bu müddətdəki xəstəlik günü (çıxılır)
        public int Son12AyEffektivIsGunu => Son12AyIsGunu - Son12AyXestelikGunu;

        // ── 12 ay intervalı (breakdown göstəriş üçün) ─────────
        public DateTime? Son12AyBaslangic { get; set; }
        public DateTime? Son12AyBitis { get; set; }

        // ── Bir günlük və ödəniş hesablaması ───────────────────
        public decimal BirGunluk { get; set; }

        // ── Staj və faiz ───────────────────────────────────────
        public int StajIl { get; set; }
        public int StajAy { get; set; }                        // 9 il 3 ay göstərmək üçün
        public decimal StajFaizi { get; set; }                 // 0.60 / 0.80 / 1.00

        // ── Ödəniş bölgüsü ─────────────────────────────────────
        public int SirketGun { get; set; }
        public int DsmfGun { get; set; }
        public decimal SirketOdenis { get; set; }              // staj faizi tətbiq olunmuş
        public decimal DsmfOdenis { get; set; }                // staj faizi tətbiq olunmuş (informativ)

        public string? Xeberdarliq { get; set; }
    }

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
        public decimal HysIsci { get; set; }
        public decimal UmumiTutulma => GelirVergisi + DsmfIsci + IssizlikIsci + Itss + HysIsci;
        public decimal NetMaas { get; set; }
        public decimal DsmfIsegoturen { get; set; }
        public decimal IssizlikIsegoturen { get; set; }
        public decimal ItssIsegoturen { get; set; }
        public decimal HysIsegoturen { get; set; }
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
        public decimal HysIsci { get; set; }
        public decimal HysIsegoturen { get; set; }
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
        public decimal HysIsegoturenFaizi { get; set; } = 15m;
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