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

        public bool SilineBilir { get; set; }
        public string? SilinmemeSebebi { get; set; }
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
        // Cari bülletənə aid sahələr
        public int IsGunSayi { get; set; }                     // Köhnə uyğunluq üçün (iş günü)
        public int TeqvimGunSayi { get; set; }                 // Cari bülletən təqvim günü (yeni qayda)

        // ── Yeni DSMF-əsaslı düstur sahələri ──────────────────
        public decimal DsmfIsciCem { get; set; }               // Son 12 ay işçi DSMF cəmi
        public decimal DsmfIsegoturenCem { get; set; }         // Son 12 ay şirkət DSMF cəmi
        public decimal DsmfEmsali { get; set; }                // ×4 (parametrdən)
        public decimal Numerator => (DsmfIsciCem + DsmfIsegoturenCem) * DsmfEmsali;

        public int Son12AyTeqvimGunu { get; set; }             // ~365
        public int Son12AyXestelikTeqvimGunu { get; set; }     // bu müddətdə xəstəlik təqvim günü
        public int Denominator => Son12AyTeqvimGunu - Son12AyXestelikTeqvimGunu;

        // ── Köhnə qayda üçün uyğunluq (S, iş günü) ────────────
        public decimal S { get; set; }
        public decimal Son12XestelikOdenisi { get; set; }
        public decimal SEffektiv => S - Son12XestelikOdenisi;

        public int Son12AyIsGunu { get; set; }
        public int Son12AyXestelikGunu { get; set; }
        public int Son12AyEffektivIsGunu => Son12AyIsGunu - Son12AyXestelikGunu;

        // ── 12 ay intervalı ────────────────────────────────────
        public DateTime? Son12AyBaslangic { get; set; }
        public DateTime? Son12AyBitis { get; set; }

        // ── Bir günlük və ödəniş ───────────────────────────────
        public decimal BirGunluk { get; set; }

        // ── Staj (köhnə qayda üçün; yeni qaydada tətbiq olunmur) ──
        public int StajIl { get; set; }
        public int StajAy { get; set; }
        public decimal StajFaizi { get; set; }

        // ── Ödəniş bölgüsü ─────────────────────────────────────
        public int SirketGun { get; set; }
        public int DsmfGun { get; set; }
        public decimal SirketOdenis { get; set; }
        public decimal DsmfOdenis { get; set; }

        // ── Addım-addım izahat (göstəriş üçün) ────────────────
        public List<XestelikIzahDto> Addimlar { get; set; } = new();
        public string? Duslar { get; set; }                    // Yığcam düstur sətri

        public string? Xeberdarliq { get; set; }
    }

    public class XestelikIzahDto
    {
        public string Addim { get; set; } = "";
        public string Izah { get; set; } = "";
        public decimal? Mebleg { get; set; }
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
        public decimal OvertimeMeblegi { get; set; }
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

        // ── Provodka (əməliyyat yazılışı) önizləməsi üçün ──
        // saxla=false dry-run-da da doldurulur ki, maaş saxlanmadan provodka qurula bilsin.
        //   BankHesabNo — rezident/qeyri-rezident ayrımı (provodkada "41015…" qeyri-rezidentdir).
        //   Detallar    — MaasNovu üzrə parçalanma (provodka sətirlərini ad-ad cəmləyir).
        public string? BankHesabNo { get; set; }
        public List<MaasDetaySetiriDto> Detallar { get; set; } = new();
    }

    // Provodka önizləməsi üçün bir maaş detalı (MaasNovu adı + məbləğ).
    public class MaasDetaySetiriDto
    {
        public string Ad { get; set; } = null!;
        public decimal Mebleg { get; set; }
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

        // Önizləmə (dry-run, saxla=false) üçün hər işçinin SERVER-tərəfi hesablanmış
        // yekun rəqəmləri. Eyni FerdiHesablaAsync engine-i ilə hesablanır, ona görə
        // bu rəqəmlər TopluHesablaEt-in bazaya yazacağı rəqəmlərlə EYNİDİR.
        public List<MaasOnizlemeFerdiDto> Ferdiler { get; set; } = new();

        // Provodka önizləməsi üçün hər işçinin TAM server-nəticəsi (Detallar + BankHesabNo daxil).
        // Browser-ə göndərilmir — yalnız server-tərəfi PravodkaOnizlemeExport istifadə edir.
        public List<MaasHesablaNeticesiDto> FerdiNeticeler { get; set; } = new();
    }

    // Toplu önizləmə — bir işçinin server-tərəfi (FerdiHesablaAsync) yekun rəqəmləri.
    // JS önizləmə bu dəyərləri göstərir → "görünən rəqəm = bazaya yazılan rəqəm" zəmanəti.
    public class MaasOnizlemeFerdiDto
    {
        public int IsciId { get; set; }
        public decimal BrutMaas { get; set; }   // = saved BrutMebleg (işəgötürən HYS daxil)
        public decimal NetMaas { get; set; }    // = saved NetMebleg
        public decimal GelirVergisi { get; set; }
        public decimal DsmfIsci { get; set; }
        public decimal IssizlikIsci { get; set; }
        public decimal Itss { get; set; }
        public decimal HysIsci { get; set; }
        public decimal UmumiTutulma { get; set; }
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
        public decimal OvertimeMeblegi { get; set; }
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

        // Qabaqcadan ödəniş məzuniyyəti vergisi (ayrıca ödənilib, aylıq cəm üçün)
        public decimal AvansGelirVergisi { get; set; }
        public decimal AvansDsmfIsci { get; set; }
        public decimal AvansIssizlikIsci { get; set; }
        public decimal AvansItss { get; set; }
        public decimal AvansBrut { get; set; }
        public decimal AvansNet { get; set; }

        public decimal BirlesikGelirVergisi => GelirVergisi + AvansGelirVergisi;
        public decimal BirlesikDsmfIsci    => DsmfIsci + AvansDsmfIsci;
        public decimal BirlesikIssizlik    => IssizlikIsci + AvansIssizlikIsci;
        public decimal BirlesikItss        => Itss + AvansItss;
        public decimal BirlesikTutulma     => BirlesikGelirVergisi + BirlesikDsmfIsci + BirlesikIssizlik + BirlesikItss + HysIsci;
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