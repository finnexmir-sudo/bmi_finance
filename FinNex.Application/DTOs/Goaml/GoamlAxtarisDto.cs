namespace FinNex.Application.DTOs.Goaml
{
    /// <summary>"Əməliyyatlar" tabının axtarış filtri.</summary>
    public class GoamlAxtarisDto
    {
        public DateTime BasTarix { get; set; }
        public DateTime SonTarix { get; set; }
        /// <summary>Minimum məbləğ (milli valyutada) — boşdursa şərt tətbiq olunmur.</summary>
        public decimal? MinMebleg { get; set; }
        /// <summary>VÖEN və ya hesab nömrəsi — tək sahə, hər ikisini axtarır.</summary>
        public string? VoenVeyaHesab { get; set; }
    }
}
