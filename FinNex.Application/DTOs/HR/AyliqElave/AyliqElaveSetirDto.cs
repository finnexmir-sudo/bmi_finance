namespace FinNex.Application.DTOs.HR.AyliqElave
{
    /// <summary>
    /// "Aylıq Əlavə — Bonus / Overtime" səhifəsində bir işçi sətrini
    /// təmsil edən DTO. Eyni DTO həm view-ya göstəriş üçün, həm də form
    /// postunda qaytarılan dəyər üçün istifadə olunur.
    /// </summary>
    public class AyliqElaveSetirDto
    {
        public int IsciId { get; set; }
        public string AdSoyad { get; set; } = "";
        public decimal Bonus { get; set; }
        public decimal Overtime { get; set; }

        /// <summary>Bu ayın maaşı artıq hesablanıb — sətir kilidlidir.</summary>
        public bool Kilidli { get; set; }
    }
}
