using FinNex.Domain.Entities.HR;

namespace FinNex.Application.DTOs.HR.Mezuniyyet
{
    public class MezuniyyetListDto
    {
        public int Id { get; set; }

        public string IsciTamAd { get; set; } = null!;
        public MezuniyyetNovu Nov { get; set; }

        public DateTime BaslamaTarixi { get; set; }
        public DateTime BitmeTarixi { get; set; }

        public MezuniyyetStatus Status { get; set; }
    }
    public class MezuniyyetDetailDto
    {
        public int Id { get; set; }

        public string IsciTamAd { get; set; } = null!;
        public string Sobe { get; set; } = null!;
        public string Vezife { get; set; } = null!;

        public MezuniyyetNovu Nov { get; set; }

        public DateTime BaslamaTarixi { get; set; }
        public DateTime BitmeTarixi { get; set; }

        public string? Qeyd { get; set; }
        public MezuniyyetStatus Status { get; set; }
    }
    public class MezuniyyetCreateDto
    {
        public int IsciId { get; set; }

        public MezuniyyetNovu Nov { get; set; }

        public DateTime BaslamaTarixi { get; set; }
        public DateTime BitmeTarixi { get; set; }

        public string? Qeyd { get; set; }
    }
    public class MezuniyyetUpdateDto
    {
        public int Id { get; set; }

        public DateTime BaslamaTarixi { get; set; }
        public DateTime BitmeTarixi { get; set; }

        public string? Qeyd { get; set; }
        public MezuniyyetStatus Status { get; set; }
    }

}
