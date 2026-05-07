using FinNex.Domain.Entities.HR;

namespace FinNex.Application.DTOs.HR.Reyting
{
    // ── Kateqoriya ───────────────────────────────────────────────
    public class ReytingKateqoriyasiDto
    {
        public int Id { get; set; }
        public string Ad { get; set; } = null!;
        public string? RengKodu { get; set; }
        public int MinXal { get; set; }
        public int MaxXal { get; set; }
        public decimal PulAmsali { get; set; }
        public decimal SaatAmsali { get; set; }
        public int Sira { get; set; }
        public bool Aktivdir { get; set; }
    }

    public class ReytingKateqoriyasiSaveDto
    {
        public int Id { get; set; }
        public string Ad { get; set; } = null!;
        public string? RengKodu { get; set; }
        public int MinXal { get; set; }
        public int MaxXal { get; set; }
        public decimal PulAmsali { get; set; }
        public decimal SaatAmsali { get; set; }
        public int Sira { get; set; }
        public bool Aktivdir { get; set; }
    }

    // ── Parametr ─────────────────────────────────────────────────
    public class ReytingParametriDto
    {
        public int Id { get; set; }
        public ReytingHadiseNovu HadiseNovu { get; set; }
        public string HadiseAdi { get; set; } = null!;
        public string Ad { get; set; } = null!;
        public int XalDeyeri { get; set; }
        public bool Aktivdir { get; set; }
    }

    public class ReytingParametriSaveDto
    {
        public int Id { get; set; }
        public ReytingHadiseNovu HadiseNovu { get; set; }
        public string Ad { get; set; } = null!;
        public int XalDeyeri { get; set; }
        public bool Aktivdir { get; set; }
    }

    // ── İşçi Reytinqi ────────────────────────────────────────────
    public class IsciReytinqiDto
    {
        public int Id { get; set; }
        public int IsciId { get; set; }
        public string IsciAd { get; set; } = null!;
        public string? Vezife { get; set; }
        public string? Departament { get; set; }
        public int Il { get; set; }
        public int Ay { get; set; }
        public string AyAd { get; set; } = null!;
        public int AvtomatikXal { get; set; }
        public int ManualXal { get; set; }
        public int CemiXal { get; set; }
        public string? KateqoriyaAd { get; set; }
        public string? KateqoriyaReng { get; set; }
        public decimal PulAmsali { get; set; }
        public decimal SaatAmsali { get; set; }
        public DateTime HesablamaTarixi { get; set; }
        public IList<ManualQeydDto> ManualQeydler { get; set; } = new List<ManualQeydDto>();
    }

    public class ManualQeydDto
    {
        public int Id { get; set; }
        public int Xal { get; set; }
        public string Sebeb { get; set; } = null!;
        public string ElavEden { get; set; } = null!;
        public DateTime Tarix { get; set; }
    }

    // ── Manual əlavə ─────────────────────────────────────────────
    public class ManualReytingElavEtDto
    {
        public int IsciId { get; set; }
        public int Il { get; set; }
        public int Ay { get; set; }
        public int Xal { get; set; }
        public string Sebeb { get; set; } = null!;
    }

    // ── İşçinin cari reytinqi (Cüzdanım üçün) ────────────────────
    public class IsciCariReytinqDto
    {
        public int CemiXal { get; set; }
        public string? KateqoriyaAd { get; set; }
        public string? KateqoriyaReng { get; set; }
        public decimal PulAmsali { get; set; }
        public decimal SaatAmsali { get; set; }
        public int Il { get; set; }
        public int Ay { get; set; }
        public IList<ManualQeydDto> SonQeydler { get; set; } = new List<ManualQeydDto>();
    }
}
