using FinNex.Domain.Entities.HR;

namespace FinNex.Application.DTOs.HR.Jeton
{
    public class JetonRedimTelebiCreateDto
    {
        public List<int> JetonIds { get; set; } = new();
        public RedimNovu RedimNovu { get; set; }
    }
}
