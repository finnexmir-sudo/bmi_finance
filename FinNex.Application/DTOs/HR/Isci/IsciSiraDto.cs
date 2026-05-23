namespace FinNex.Application.DTOs.HR.Isci
{
    /// <summary>
    /// "İşçi Sıralaması" səhifəsində bir sətri təmsil edən DTO.
    /// </summary>
    public class IsciSiraDto
    {
        public int IsciId { get; set; }
        public string AdSoyad { get; set; } = "";
        public string? VezifeAd { get; set; }
        public string? DepartamentAd { get; set; }
        public int Sira { get; set; }
    }
}
