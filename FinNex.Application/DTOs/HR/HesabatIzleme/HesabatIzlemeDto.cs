using FinNex.Domain.Entities.HR;

namespace FinNex.Application.DTOs.HR.HesabatIzleme
{
    // ── Şablon DTOs ──
    public class HesabatSablonListDto
    {
        public int Id { get; set; }
        public string Ad { get; set; } = null!;
        public string? Tesvir { get; set; }
        public HesabatTezlik Tezlik { get; set; }
        public HesabatKateqoriya Kateqoriya { get; set; }
        public HesabatPrioritet Prioritet { get; set; }
        public int SonTarixGunu { get; set; }
        public TimeSpan SonTarixSaati { get; set; }
        public string MesulIsciAd { get; set; } = null!;
        public int MesulIsciId { get; set; }
        public string DepartamentAd { get; set; } = null!;
        public int DepartamentId { get; set; }
        public bool Aktivdir { get; set; }

        public string TezlikText => Tezlik switch
        {
            HesabatTezlik.Gunluk => "Günlük",
            HesabatTezlik.Heftelik => "Həftəlik",
            HesabatTezlik.Aylik => "Aylıq",
            HesabatTezlik.Rubluk => "Rüblük",
            HesabatTezlik.Illik => "İllik",
            _ => "-"
        };

        public string KateqoriyaText => Kateqoriya switch
        {
            HesabatKateqoriya.Vergi => "Vergi",
            HesabatKateqoriya.Maas => "Maaş",
            HesabatKateqoriya.Bank => "Bank",
            HesabatKateqoriya.Kassa => "Kassa",
            HesabatKateqoriya.Dovlet => "Dövlət",
            HesabatKateqoriya.Daxili => "Daxili",
            _ => "-"
        };

        public string PrioritetText => Prioritet switch
        {
            HesabatPrioritet.Yuksek => "Yüksək",
            HesabatPrioritet.Orta => "Orta",
            HesabatPrioritet.Asagi => "Aşağı",
            _ => "-"
        };
    }

    public class HesabatSablonCreateDto
    {
        public string Ad { get; set; } = null!;
        public string? Tesvir { get; set; }
        public HesabatTezlik Tezlik { get; set; }
        public HesabatKateqoriya Kateqoriya { get; set; }
        public HesabatPrioritet Prioritet { get; set; } = HesabatPrioritet.Orta;
        public int SonTarixGunu { get; set; }
        public TimeSpan SonTarixSaati { get; set; }
        public int MesulIsciId { get; set; }
        public int DepartamentId { get; set; }
    }

    // ── Tapşırıq DTOs ──
    public class HesabatTapshiriqListDto
    {
        public int Id { get; set; }
        public int SablonId { get; set; }
        public string SablonAd { get; set; } = null!;
        public string? SablonTesvir { get; set; }
        public HesabatTezlik Tezlik { get; set; }
        public HesabatKateqoriya Kateqoriya { get; set; }
        public HesabatPrioritet Prioritet { get; set; }
        public string MesulIsciAd { get; set; } = null!;
        public string DepartamentAd { get; set; } = null!;
        public int DepartamentId { get; set; }

        public DateTime DovrBaslangic { get; set; }
        public DateTime DovrSon { get; set; }
        public DateTime Deadline { get; set; }

        public HesabatTapshiriqStatus Status { get; set; }
        public string? IcraEdenAd { get; set; }
        public DateTime? IcraTarixi { get; set; }
        public string? Qeyd { get; set; }

        public string StatusText => Status switch
        {
            HesabatTapshiriqStatus.Gozleyir => "Gözləyir",
            HesabatTapshiriqStatus.Icrada => "İcrada",
            HesabatTapshiriqStatus.Tamamlandi => "Tamamlandı",
            HesabatTapshiriqStatus.Gecikib => "Gecikib",
            _ => "-"
        };

        public string TezlikText => Tezlik switch
        {
            HesabatTezlik.Gunluk => "Günlük",
            HesabatTezlik.Heftelik => "Həftəlik",
            HesabatTezlik.Aylik => "Aylıq",
            HesabatTezlik.Rubluk => "Rüblük",
            HesabatTezlik.Illik => "İllik",
            _ => "-"
        };

        public string KateqoriyaText => Kateqoriya switch
        {
            HesabatKateqoriya.Vergi => "Vergi",
            HesabatKateqoriya.Maas => "Maaş",
            HesabatKateqoriya.Bank => "Bank",
            HesabatKateqoriya.Kassa => "Kassa",
            HesabatKateqoriya.Dovlet => "Dövlət",
            HesabatKateqoriya.Daxili => "Daxili",
            _ => "-"
        };

        public string PrioritetText => Prioritet switch
        {
            HesabatPrioritet.Yuksek => "Yüksək",
            HesabatPrioritet.Orta => "Orta",
            HesabatPrioritet.Asagi => "Aşağı",
            _ => "-"
        };

        public bool Gecikibmi => Status != HesabatTapshiriqStatus.Tamamlandi && Deadline < DateTime.Now;

        public string DovrText => Tezlik switch
        {
            HesabatTezlik.Gunluk => DovrBaslangic.ToString("dd.MM.yyyy"),
            HesabatTezlik.Heftelik => $"{DovrBaslangic:dd.MM} - {DovrSon:dd.MM.yyyy}",
            HesabatTezlik.Aylik => DovrBaslangic.ToString("MMMM yyyy"),
            HesabatTezlik.Rubluk => $"R{((DovrBaslangic.Month - 1) / 3) + 1} {DovrBaslangic.Year}",
            HesabatTezlik.Illik => DovrBaslangic.Year.ToString(),
            _ => "-"
        };
    }

    // ── Dashboard VM ──
    public class HesabatIzlemeDashboardDto
    {
        public int Umumi { get; set; }
        public int Tamamlanan { get; set; }
        public int Gozleyen { get; set; }
        public int Geciken { get; set; }
        public int TamamlanmaFaizi { get; set; }

        public List<HesabatTapshiriqListDto> BugunkuTapshiriqlar { get; set; } = new();
        public List<HesabatTapshiriqListDto> BuAyTapshiriqlar { get; set; } = new();
        public List<HesabatTapshiriqListDto> GecikenTapshiriqlar { get; set; } = new();
        public List<HesabatSablonListDto> Sablonlar { get; set; } = new();
    }
}
