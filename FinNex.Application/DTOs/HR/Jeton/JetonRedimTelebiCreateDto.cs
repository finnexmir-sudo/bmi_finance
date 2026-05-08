using FinNex.Domain.Entities.HR;

namespace FinNex.Application.DTOs.HR.Jeton
{
    public class JetonRedimTelebiCreateDto
    {
        public List<int> JetonIds { get; set; } = new();
        public RedimNovu RedimNovu { get; set; }

        // Yalnız RedimNovu == Icaze üçün məcburi:
        // HR təsdiqdən sonra bu məlumatdan Icaze qeydi yaranır.
        public DateTime? IcazeTarixi { get; set; }
        public TimeSpan? BaslamaSaati { get; set; }
        public TimeSpan? BitisSaati { get; set; }
    }
}
