using FinNex.Domain.Entities.Marif;

namespace FinNex.Application.DTOs.Marif
{
    /// <summary>Admin → siyahı ekranı.</summary>
    public class MarifSualSetiListDto
    {
        public int Id { get; set; }
        public string Basliq { get; set; } = "";
        public MarifSetStatusu Status { get; set; }
        public int SualSayi { get; set; }
        public int GonderilenIsciSayi { get; set; }
        public int TamamlayanIsciSayi { get; set; }
        public DateTime YaradilmaTarixi { get; set; }
    }

    /// <summary>Admin → detal ekranı (sualların özü, redaktə/göndərmədən əvvəl və sonra).</summary>
    public class MarifSualSetiDetailDto
    {
        public int Id { get; set; }
        public string Basliq { get; set; } = "";
        public MarifSetStatusu Status { get; set; }

        // 28.10.2026 — Qaralamanı "Yarat" ekranında yenidən açıb redaktə etmək
        // (naviqasiyadan sonra itən generasiyanı bərpa etmək) üçün mənbəni də
        // tanımaq lazımdır — əvvəl bu üç sahə DTO-da yox idi.
        public int? HRDaxiliQaydaId { get; set; }
        public int? HRQanunFaylId { get; set; }
        public string? MenbeMetn { get; set; }

        public List<MarifSualDto> Suallar { get; set; } = new();
        public List<MarifTesyinatNeticeDto> Tesyinatlar { get; set; } = new();
    }

    /// <summary>Admin → detal ekranında hər işçinin nəticəsi (kim tamamlayıb, neçə düz).</summary>
    public class MarifTesyinatNeticeDto
    {
        public int TesyinatId { get; set; }
        public int IsciId { get; set; }
        public string IsciAdi { get; set; } = "";
        public bool TamamlandiMi { get; set; }
        public DateTime GonderilmeTarixi { get; set; }
        public DateTime? TamamlanmaTarixi { get; set; }
        public int CoxSecimliDuzgunSayi { get; set; }
        public int CoxSecimliCemi { get; set; }
    }

    /// <summary>Admin → qayda atıb sual hazırlatmaq üçün sorğu. Üç mənbə — yalnız biri doldurulur.</summary>
    public class MarifSetiGenerasiyaDto
    {
        /// <summary>Mövcud HR Daxili Qaydasından mənbə seçilibsə (ixtiyari).</summary>
        public int? HRDaxiliQaydaId { get; set; }

        /// <summary>Mövcud HR Qanun faylından mənbə seçilibsə (ixtiyari).</summary>
        public int? HRQanunFaylId { get; set; }

        /// <summary>HR-in birbaşa yapışdırdığı/yüklədiyi mətn (ixtiyari — yuxarıdakılar yoxdursa məcburidir).</summary>
        public string? MenbeMetn { get; set; }

        public int SualSayi { get; set; } = 5;
    }

    /// <summary>Admin → redaktə olunmuş sualları yadda saxlamaq üçün sorğu.</summary>
    public class MarifSualSetiYaratDto
    {
        public string Basliq { get; set; } = "";
        public int? HRDaxiliQaydaId { get; set; }
        public int? HRQanunFaylId { get; set; }
        public string? MenbeMetn { get; set; }
        public List<MarifSualDto> Suallar { get; set; } = new();
    }

    /// <summary>Admin → seçilmiş işçilərə göndərmək üçün.</summary>
    public class MarifGonderDto
    {
        public int SualSetiId { get; set; }
        public List<int> IsciIdler { get; set; } = new();
    }
}
