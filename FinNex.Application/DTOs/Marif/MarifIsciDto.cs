using FinNex.Domain.Entities.Marif;

namespace FinNex.Application.DTOs.Marif
{
    /// <summary>İşçi → "Mənə göndərilən testlər" siyahısı.</summary>
    public class MarifIsciTesyinatListDto
    {
        public int TesyinatId { get; set; }
        public string Basliq { get; set; } = "";
        public DateTime GonderilmeTarixi { get; set; }
        public bool TamamlandiMi { get; set; }
        public int SualSayi { get; set; }
    }

    /// <summary>İşçi → tək sualı cavablamaq üçün göstərilən forma (düzgün cavab YOXDUR).</summary>
    public class MarifIsciSualDto
    {
        public int SualId { get; set; }
        public MarifSualTipi Tip { get; set; }
        public string SualMetni { get; set; } = "";
        public string? VariantA { get; set; }
        public string? VariantB { get; set; }
        public string? VariantC { get; set; }
        public string? VariantD { get; set; }
        public int Sira { get; set; }

        /// <summary>Bu sual artıq cavablanıbsa, işçinin əvvəl verdiyi cavab (yenidən cavablamasın deyə).</summary>
        public bool CavablanibMi { get; set; }
    }

    /// <summary>İşçi → testin tam görüntüsü.</summary>
    public class MarifIsciSualSetiDto
    {
        public int TesyinatId { get; set; }
        public string Basliq { get; set; } = "";
        public bool TamamlandiMi { get; set; }
        public List<MarifIsciSualDto> Suallar { get; set; } = new();
    }

    public class MarifCavabVerDto
    {
        public int TesyinatId { get; set; }
        public int SualId { get; set; }
        public string Cavab { get; set; } = "";
    }

    /// <summary>Cavabdan DƏRHAL SONRA göstərilən nəticə — doğru cavab + izah.</summary>
    public class MarifCavabNeticeDto
    {
        /// <summary>CoxSecimli-də true/false; Acig-də null (qiymətləndirilmir, yalnız nümunə göstərilir).</summary>
        public bool? DuzgunduMu { get; set; }
        public string DuzgunCavabMetni { get; set; } = "";
        public string IzahMetni { get; set; } = "";

        /// <summary>Bütün suallar bitibsə true — işçi ekrana "test tamamlandı" göstərə bilsin.</summary>
        public bool TestTamamlandi { get; set; }
    }
}
