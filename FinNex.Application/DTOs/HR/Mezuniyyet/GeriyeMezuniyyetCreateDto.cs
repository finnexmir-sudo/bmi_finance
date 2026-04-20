using System.ComponentModel.DataAnnotations;
using FinNex.Domain.Entities.HR;

namespace FinNex.Application.DTOs.HR.Mezuniyyet
{
    /// <summary>
    /// HR tərəfindən keçmiş tarixlər üçün məzuniyyətin geriyə qeyd edilməsi.
    /// İşçi emergency səbəbilə işdə olmadıqda, sonradan HR bu formla rəsmiləşdirir.
    /// Təsdiq axınından keçmir — bütün addımlar HR adından "təsdiqlənmiş"
    /// kimi yazılır (audit üçün HrId doldurulur), Qeyd sahəsinə "[Geriyə qeyd]" taqı
    /// avtomatik əlavə olunur.
    /// </summary>
    public class GeriyeMezuniyyetCreateDto
    {
        [Required(ErrorMessage = "İşçi seçilməlidir.")]
        public int IsciId { get; set; }

        [Required(ErrorMessage = "Məzuniyyət növü seçilməlidir.")]
        public MezuniyyetNovu Nov { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime BaslamaTarixi { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime BitmeTarixi { get; set; }

        [Required(ErrorMessage = "Səbəb qeyd edilməlidir.")]
        [MinLength(5, ErrorMessage = "Səbəb ən azı 5 simvoldan ibarət olmalıdır.")]
        [MaxLength(500)]
        public string Sebeb { get; set; } = "";
    }
}
