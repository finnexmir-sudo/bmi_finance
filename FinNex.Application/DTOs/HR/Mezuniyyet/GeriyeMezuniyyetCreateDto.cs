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

        [MaxLength(500)]
        public string? Sebeb { get; set; }

        /// <summary>
        /// Əmr nömrəsi suffiksi ("frekans") — geriyə məzuniyyəti normal seriyadan
        /// fərqləndirmək üçün. Default "G" (Geriyə). HR seçimə uyğun dəyişdirə bilər
        /// (məs. "X" — xüsusi hal, "T" — təcili). EmrRegem avtomatik artırılır,
        /// EmrSuffiks isə bu sahədən gəlir → əmr nömrəsi: "K/M 13G 2026" kimi.
        /// </summary>
        [MaxLength(4, ErrorMessage = "Suffiks ən çox 4 simvol ola bilər.")]
        public string? EmrSuffiks { get; set; } = "G";
    }
}
