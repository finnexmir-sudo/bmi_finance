namespace FinNex.Application.DTOs.HR.Imtiyaz
{
    /// <summary>
    /// İşçiyə verilmiş fərdi imtiyaz — oxuma DTO-su. HR profilindəki "İmtiyazlar"
    /// sekmesində və işçinin öz Dashboard kartında istifadə olunur.
    /// </summary>
    public class IsciImtiyazDto
    {
        public int Id { get; set; }
        public int IsciId { get; set; }

        public string Baslik { get; set; } = "";

        public DayOfWeek? HeftaGunu { get; set; }
        public string HeftaGunuAdi => HeftaGunu switch
        {
            DayOfWeek.Monday => "Bazar ertəsi",
            DayOfWeek.Tuesday => "Çərşənbə axşamı",
            DayOfWeek.Wednesday => "Çərşənbə",
            DayOfWeek.Thursday => "Cümə axşamı",
            DayOfWeek.Friday => "Cümə",
            DayOfWeek.Saturday => "Şənbə",
            DayOfWeek.Sunday => "Bazar",
            null => "Hər gün",
            _ => ""
        };

        public TimeSpan? BaslamaSaati { get; set; }
        public TimeSpan? BitisSaati { get; set; }

        public string? Aciqlama { get; set; }

        public DateTime BaslamaTarixi { get; set; }
        public DateTime? BitmeTarixi { get; set; }
        public bool Aktivdir { get; set; }
    }

    public class IsciImtiyazCreateDto
    {
        public int Id { get; set; }
        public int IsciId { get; set; }
        public string Baslik { get; set; } = "";
        public DayOfWeek? HeftaGunu { get; set; }
        public TimeSpan? BaslamaSaati { get; set; }
        public TimeSpan? BitisSaati { get; set; }
        public string? Aciqlama { get; set; }
        public DateTime BaslamaTarixi { get; set; } = DateTime.Today;
        public DateTime? BitmeTarixi { get; set; }
    }
}
