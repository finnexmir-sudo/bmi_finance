using FinNex.Domain.Entities.HR;

namespace FinNex.Application.DTOs.HR.Mezuniyyet
{
    // Təsdiq və ya düzəliş üçün
    public class MezuniyyetUpdateDto
    {
        public int Id { get; set; }

        // Redaktə üçün tarixlər (Vurğuladığınız hissə)
        public DateTime BaslamaTarixi { get; set; }
        public DateTime BitmeTarixi { get; set; }

        public int? EvezEdenIsciId { get; set; }
        public MezuniyyetNovu Nov { get; set; }
        public string? Qeyd { get; set; }

        // Təsdiq prosesi üçün backend-də yenilənəcək sahələr
        public MezuniyyetStatus Status { get; set; }
        public string? ImtinaSebebi { get; set; }
    }
}
