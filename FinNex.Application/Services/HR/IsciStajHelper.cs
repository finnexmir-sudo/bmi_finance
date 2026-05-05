using System.Text.Json;
using FinNex.Domain.Entities.HR;

namespace FinNex.Application.Services.HR
{
    /// <summary>
    /// İşçinin ümumi iş stajını hesablayır:
    ///   total = bu bankda staj (IsheQebulTarixi → ref) + əvvəlki iş yerləri (kitabçə)
    ///
    /// Əvvəlki dövrlər Isci.EvvelkiStajPeriodlari (JSON) sahəsində saxlanılır:
    ///   [{"s":"2010-01-15","e":"2015-06-30"}, ...]
    /// </summary>
    public static class IsciStajHelper
    {
        public class Period
        {
            public string? s { get; set; }
            public string? e { get; set; }
        }

        public class StajNeticesi
        {
            public int Il { get; set; }
            public int Ay { get; set; }
            public int Gun { get; set; }
            public int Faiz { get; set; }
            public int EvvelkiIl { get; set; }
            public int EvvelkiAy { get; set; }
            public int EvvelkiGun { get; set; }
            public int BankIl { get; set; }
            public int BankAy { get; set; }
            public int BankGun { get; set; }
        }

        /// <summary>
        /// İşçinin ümumi iş stajını referans tarixinə görə hesablayır.
        /// </summary>
        public static StajNeticesi Hesabla(Isci isci, DateTime referans)
        {
            var bank = AylariNormalize(GunFerqi(isci.IsheQebulTarixi.Date, referans.Date));

            (int il, int ay, int gun) evvelki = (0, 0, 0);
            if (!string.IsNullOrWhiteSpace(isci.EvvelkiStajPeriodlari))
            {
                try
                {
                    var liste = JsonSerializer.Deserialize<List<Period>>(isci.EvvelkiStajPeriodlari!) ?? new();
                    foreach (var p in liste)
                    {
                        if (DateTime.TryParse(p.s, out var s) && DateTime.TryParse(p.e, out var en) && en > s)
                        {
                            var d = GunFerqi(s.Date, en.Date);
                            evvelki.il += d.il;
                            evvelki.ay += d.ay;
                            evvelki.gun += d.gun;
                        }
                    }
                    evvelki = AylariNormalize(evvelki);
                }
                catch { /* korlanmış JSON — sıfırla */ }
            }

            // Cəm
            var cem = AylariNormalize((bank.il + evvelki.il, bank.ay + evvelki.ay, bank.gun + evvelki.gun));

            int faiz = cem.il < 8 ? 60 : cem.il < 12 ? 80 : 100;

            return new StajNeticesi
            {
                Il = cem.il,
                Ay = cem.ay,
                Gun = cem.gun,
                Faiz = faiz,
                EvvelkiIl = evvelki.il,
                EvvelkiAy = evvelki.ay,
                EvvelkiGun = evvelki.gun,
                BankIl = bank.il,
                BankAy = bank.ay,
                BankGun = bank.gun
            };
        }

        // İki tarix arası dövrü (il, ay, gün) qaytarır.
        private static (int il, int ay, int gun) GunFerqi(DateTime baslama, DateTime bitme)
        {
            if (bitme <= baslama) return (0, 0, 0);
            int il = bitme.Year - baslama.Year;
            int ay = bitme.Month - baslama.Month;
            int gun = bitme.Day - baslama.Day;
            if (gun < 0)
            {
                ay--;
                gun += DateTime.DaysInMonth(bitme.AddMonths(-1).Year, bitme.AddMonths(-1).Month);
            }
            if (ay < 0) { il--; ay += 12; }
            return (il, ay, gun);
        }

        // Daşan günləri/ayları normalize edir.
        private static (int il, int ay, int gun) AylariNormalize((int il, int ay, int gun) v)
        {
            v.ay += v.gun / 30;
            v.gun = v.gun % 30;
            v.il += v.ay / 12;
            v.ay = v.ay % 12;
            return v;
        }
    }
}
