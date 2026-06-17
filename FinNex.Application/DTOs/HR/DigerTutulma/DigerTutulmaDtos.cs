namespace FinNex.Application.DTOs.HR.DigerTutulma
{
    /// <summary>Siyahı sətri.</summary>
    public class DigerTutulmaListDto
    {
        public int Id { get; set; }
        public int IsciId { get; set; }
        public string IsciAdSoyad { get; set; } = null!;
        public decimal Mebleg { get; set; }
        public int MuddetAy { get; set; }
        public decimal AyliqMebleg { get; set; }
        public int BaslamaIl { get; set; }
        public int BaslamaAy { get; set; }
        public int BitmeIl { get; set; }
        public int BitmeAy { get; set; }

        /// <summary>Cədvəl üzrə cari aya qədər tutulan ay sayı (0..MuddetAy).</summary>
        public int TutulubSayi { get; set; }
        /// <summary>Hələ tutulmamış qalıq məbləğ.</summary>
        public decimal QalanMebleg { get; set; }

        public string? Aciqlama { get; set; }
        public bool Aktiv { get; set; }
        public DateTime YaradilmaTarixi { get; set; }
    }

    /// <summary>Yaratma input-u.</summary>
    public class DigerTutulmaYaratDto
    {
        public int IsciId { get; set; }
        public decimal Mebleg { get; set; }
        public int MuddetAy { get; set; } = 12;
        public int BaslamaIl { get; set; }
        public int BaslamaAy { get; set; }
        public string? Aciqlama { get; set; }
    }
}
