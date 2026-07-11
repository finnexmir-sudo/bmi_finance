using System.Globalization;
using FinNex.Application.DTOs.Emeliyyat;

namespace FinNex.Application.Helpers.Emeliyyat;

/// <summary>
/// BMI "Pul Kocurmesi" formasının cevirme() məntiqinin dəqiq portu.
/// Seçim (Hesab açmadan / Hesab və mədaxil / Hesabdan) × Köçürülən valyuta
/// (USD/Avro/Rial/Rubl) × Mədaxil valyuta (USD/Avro/AZN) kombinasiyasına görə
/// debet/kredit muhasibat sətirlərini yaradır. Komissiya sabit 10 vahid;
/// diling (kurs fərqi) zərər/gəlirə yazılır. Hesab nömrələri BMI ilə eynidir.
/// </summary>
public static class PulKocurmeVoucher
{
    // Hesab nömrələri (BMI cevirme() ilə eyni)
    private const string Kusd = "10020010000000100000";
    private const string Keuro = "10020020000000100000";
    private const string Kazn = "10010000000000100000";
    private const string Hacusd = "45023010010000400000";
    private const string Haceuro = "45023020020000400000";
    private const string Haazn = "45013000000000400001";
    private const string Xvalqisatqi = "45011000010000400000";
    private const string HacIIR = "45023040020000400000";
    private const string Kochesab = "15025040001451500000";
    private const string Mushesolanda = "45021040000000400000";
    private const string HacRUB = "45023020020000400000";
    private const string KochesabRUB = "15025030000975000000";
    private const string MushRUB = "45021030000000400000";
    private const string Xhusd = "67023010000000600000";
    private const string Xheuro = "67023020000000600000";
    private const string Xhazn = "67013000000000600000";
    private const string Zerer = "88010000000001200000";
    private const string Gelir = "68010000000001200000";
    private const string Husd = "35025010000001600000";
    private const string Heuro = "35025020000001600000";

    private const decimal XH = 10m;   // sabit xidmət haqqı

    public class Input
    {
        public string Secim { get; set; } = "acmadan";     // acmadan / vemedaxil / hesabdan
        public string Kocurulen { get; set; } = "USD";      // USD / Avro / Rial / Rubl
        public string Medaxil { get; set; } = "USD";        // USD / Avro / AZN (Rial/Rubl üçün)
        public decimal Mebleg { get; set; }
        public decimal IranRial { get; set; }               // kurs (1 vahid = N Rial/Rubl)
        public decimal RialCbar { get; set; }
        public decimal ValyutaCbar { get; set; }
        public string? MusteriHesabi { get; set; }
        public string? BankAdi { get; set; }
        public string? Filial { get; set; }
        public string? Hevale { get; set; }
        public string? Meqsed { get; set; }
        public string? AlanAdi { get; set; }
        public string GonderenTamAd { get; set; } = "";     // "(Ad Soyad Ata)"
    }

    private static string F(decimal v) => v.ToString(CultureInfo.InvariantCulture);
    private static decimal R(decimal v) => Math.Round(v, MidpointRounding.AwayFromZero);

    public static IList<MuhasibatSetirDto> Qur(Input inp)
    {
        var rows = new List<MuhasibatSetirDto>();
        void Add(string db, string kr, decimal meb, string tey) =>
            rows.Add(new MuhasibatSetirDto { Debet = db, Kredit = kr, Mebleg = meb, Teyinat = tey });

        var mbl = inp.Mebleg;
        var xhCem = mbl + XH;
        var hev = inp.Hevale ?? "";
        var bankFilial = $"{inp.BankAdi} {inp.Filial}".Trim();
        var gond = inp.GonderenTamAd;   // "(...)"

        // Sadə təyinatlar (USD/EUR)
        string t1 = "köçürmə və x/h üçün mədaxil";
        string tSimple1 = $"{t1} {gond}";

        // Valyutaya görə kassa/tranzit/xh/hesablaşma
        (string k, string hac, string xh, string h) Val(string c) => c switch
        {
            "USD" => (Kusd, Hacusd, Xhusd, Husd),
            "Avro" => (Keuro, Haceuro, Xheuro, Heuro),
            "AZN" => (Kazn, Haazn, Xhazn, ""),
            _ => (Kusd, Hacusd, Xhusd, Husd)
        };
        string CurWord(string c) => c switch { "USD" => "ABŞ dolları", "Avro" => "Avro", "AZN" => "AZN", _ => c };

        bool rialRubl = inp.Kocurulen is "Rial" or "Rubl";

        // ---- Sadə USD / Avro köçürmə ----
        if (!rialRubl)
        {
            var (k, hac, xh, h) = Val(inp.Kocurulen);
            string musteri = inp.MusteriHesabi ?? "";

            if (inp.Secim == "acmadan")
            {
                Add(k, hac, xhCem, tSimple1);
                Add(hac, h, mbl, $"{hev} {bankFilial} {bankFilial} {gond}");
                Add(hac, xh, XH, $"{hev} {bankFilial} {bankFilial} {gond} x/h");
            }
            else if (inp.Secim == "vemedaxil")
            {
                Add(k, musteri, xhCem, tSimple1);
                Add(musteri, h, mbl, $"{hev} {bankFilial}  {gond}");
                Add(musteri, xh, XH, $"{hev} {bankFilial}  {gond} x/h");
            }
            else // hesabdan
            {
                Add(musteri, h, mbl, $"{hev} {bankFilial}  {gond}");
                Add(musteri, xh, XH, $"{hev} {bankFilial}  {gond} x/h");
            }
            return rows;
        }

        // ---- Rial / Rubl köçürmə (mədaxil valyutası ilə maliyyələşir) ----
        var (mk, mhac, mxh, _) = Val(inp.Medaxil);           // funding kassa/tranzit/xh
        bool rubl = inp.Kocurulen == "Rubl";
        string hacFX = rubl ? HacRUB : HacIIR;
        string koch = rubl ? KochesabRUB : Kochesab;
        string mush = rubl ? MushRUB : Mushesolanda;
        string fxLabel = rubl ? "Rubl" : "İran Rialı";
        string curWord = CurWord(inp.Medaxil);
        string medText = inp.Medaxil;                        // "1 USD= ..."
        string musteriH = inp.MusteriHesabi ?? "";

        decimal rcbar = inp.RialCbar, vcbar = inp.ValyutaCbar, irial = inp.IranRial;
        decimal cross = rcbar != 0 ? vcbar / rcbar : 0m;
        decimal gedrial = mbl * irial;

        string tKurs = $"1 {medText}= {F(irial)} {fxLabel} {hev} {gond}";
        string tReceiver = $"{hev} {inp.BankAdi} {gond} {inp.Meqsed} vəsaiti alan şəxs {inp.AlanAdi}";
        string tReceiverXh = $"{hev} {inp.BankAdi} {gond} {inp.Meqsed} vəsaiti alan şəxs x/h";

        // Diling (kurs fərqi) sətri
        void DilingAdd()
        {
            if (cross == irial || rcbar == 0) return;
            decimal fark, ferq;
            if (cross < irial)
            {
                fark = irial - cross;
                ferq = fark * mbl * rcbar;
                Add(Zerer, Xvalqisatqi, R(ferq * 100m) / 100m,
                    $"{curWord}-{fxLabel} dilinq fərqi {F(cross)} - {F(irial)} =-{F(R(fark))} ({F(mbl)} {curWord}) {hev} {gond}");
            }
            else
            {
                fark = cross - irial;
                ferq = fark * mbl * rcbar;
                Add(Xvalqisatqi, Gelir, R(ferq * 100m) / 100m,
                    $"{curWord}-{fxLabel} dilinq fərqi {F(cross)} - {F(irial)} ={F(R(fark))} ({F(mbl)} {curWord}) {hev} {gond}");
            }
        }

        if (inp.Secim == "acmadan")
        {
            // db5: AZN maliyyələşmədə haazn, əks halda hacFX (BMI quirk)
            string db5 = inp.Medaxil == "AZN" ? mhac : hacFX;
            Add(mk, mhac, xhCem, tSimple1);
            Add(mhac, Xvalqisatqi, R(mbl), tKurs);
            Add(Xvalqisatqi, hacFX, R(gedrial), tKurs);
            Add(hacFX, koch, R(gedrial), tReceiver);
            Add(db5, mxh, R(XH), tReceiverXh);
            DilingAdd();
        }
        else if (inp.Secim == "vemedaxil")
        {
            Add(mk, musteriH, xhCem, tSimple1);
            Add(musteriH, Xvalqisatqi, R(mbl), tKurs);
            Add(Xvalqisatqi, mush, R(gedrial), tKurs);
            Add(mush, koch, R(gedrial), tReceiver);
            Add(musteriH, mxh, R(XH), tReceiverXh);
            DilingAdd();
        }
        else // hesabdan (kassa ayağı yoxdur)
        {
            Add(musteriH, Xvalqisatqi, R(mbl), tKurs);
            Add(Xvalqisatqi, mush, R(gedrial), tKurs);
            Add(mush, koch, R(gedrial), tReceiver);
            Add(musteriH, mxh, R(XH), tReceiverXh);
            DilingAdd();
        }

        return rows;
    }
}
