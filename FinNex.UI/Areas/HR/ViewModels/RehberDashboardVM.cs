using FinNex.Domain.Entities.HR;

namespace FinNex.UI.Areas.HR.ViewModels
{
    public class RehberDashboardVM
    {
        // ── Ümumi Statistika ──
        public int UmumiIsciSayi { get; set; }
        public int AktivIsciSayi { get; set; }
        public int MezuniyyetdeIsciSayi { get; set; }
        public int IshdenCixanSayi { get; set; }

        public int KisiSayi { get; set; }
        public int QadinSayi { get; set; }

        public int BuAyIsheQebulSayi { get; set; }
        public int BuAyIshdenAyrilanSayi { get; set; }

        // ── Davamiyyət ──
        public int BugunIshde { get; set; }
        public int BugunGeciken { get; set; }
        public int BugunQayib { get; set; }
        public int BugunIcazeli { get; set; }
        public int BugunMezuniyyetde { get; set; }
        public int DavamiyyetFaizi { get; set; }

        // ── Məzuniyyət & İcazə ──
        public int HazirdaMezuniyyetde { get; set; }
        public int BuHefteBitenMezuniyyet { get; set; }
        public int GozleyenMezuniyyetTesdiqi { get; set; }
        public int GozleyenIcazeTesdiqi { get; set; }
        public int IllikMezuniyyetSayi { get; set; }
        public int XestelikMezuniyyetSayi { get; set; }
        public int EzamiyyetSayi { get; set; }
        public List<MezuniyyetdeOlanDto> MezuniyyetdeOlanlar { get; set; } = new();
        public List<GozleyenTesdiqDto> GozleyenMezuniyyetler { get; set; } = new();
        public List<GozleyenTesdiqDto> GozleyenIcazeler { get; set; } = new();

        // ── Departament Analitikası ──
        public List<DepartamentStatDto> DepartamentStat { get; set; } = new();

        // ── Maaş / Maliyyə ──
        public decimal UmumiBrutMaas { get; set; }
        public decimal UmumiNetMaas { get; set; }
        public decimal OrtaMaas { get; set; }
        public int MaasLayihe { get; set; }
        public int MaasTesdiqlendi { get; set; }
        public int MaasOdenildi { get; set; }
        public List<MaasDeyisiklikDto> SonMaasDeyisiklikleri { get; set; } = new();

        // ── Yaş & Staj ──
        public int Yas20_30 { get; set; }
        public int Yas30_40 { get; set; }
        public int Yas40_50 { get; set; }
        public int Yas50Plus { get; set; }

        public int Staj0_1 { get; set; }
        public int Staj1_3 { get; set; }
        public int Staj3_5 { get; set; }
        public int Staj5Plus { get; set; }

        // ── Vəzifə paylanması ──
        public List<VezifeStatDto> VezifeStat { get; set; } = new();

        // ── Son girişlər ──
        public List<SonGirisDto> SonGirisler { get; set; } = new();
    }

    public class MezuniyyetdeOlanDto
    {
        public string AdSoyad { get; set; } = null!;
        public string Departament { get; set; } = null!;
        public string Nov { get; set; } = null!;
        public DateTime BitmeTarixi { get; set; }
    }

    public class GozleyenTesdiqDto
    {
        public int Id { get; set; }
        public string AdSoyad { get; set; } = null!;
        public string Departament { get; set; } = null!;
        public string Tarix { get; set; } = null!;
    }

    public class DepartamentStatDto
    {
        public string Ad { get; set; } = null!;
        public int IsciSayi { get; set; }
        public int KisiSayi { get; set; }
        public int QadinSayi { get; set; }
    }

    public class MaasDeyisiklikDto
    {
        public string AdSoyad { get; set; } = null!;
        public decimal KohneMaas { get; set; }
        public decimal YeniMaas { get; set; }
        public DateTime Tarix { get; set; }
    }

    public class VezifeStatDto
    {
        public string Ad { get; set; } = null!;
        public int IsciSayi { get; set; }
    }

    public class SonGirisDto
    {
        public string Ad { get; set; } = null!;
        public string Departament { get; set; } = null!;
        public string? GirisVaxti { get; set; }
        public string? CixisVaxti { get; set; }
        public int Status { get; set; }
    }
}
