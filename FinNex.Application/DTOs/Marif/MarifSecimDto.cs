namespace FinNex.Application.DTOs.Marif
{
    /// <summary>İşçi çoxlu-seçim siyahısı (IsciSec ekranı) — anonim tip/dynamic əvəzinə
    /// açıq DTO (Razor view-da `dynamic` ilə anonim tipə müraciət assembly sərhədi
    /// riski daşıyır, bax CLAUDE.md-dəki oxşar tələlər).</summary>
    public class MarifIsciSecimDto
    {
        public int Id { get; set; }
        public string Ad { get; set; } = "";
        public string Soyad { get; set; } = "";
    }

    /// <summary>Mənbə dropdown-u (Daxili Qayda / Qanun Faylı) — hazır formatlanmış etiketlə.</summary>
    public class MarifMenbeSecimDto
    {
        public int Id { get; set; }
        public string Etiket { get; set; } = "";
    }
}
