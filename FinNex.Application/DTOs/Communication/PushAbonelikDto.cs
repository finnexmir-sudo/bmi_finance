namespace FinNex.Application.DTOs.Communication
{
    public class PushAbonelikDto
    {
        public string Endpoint { get; set; } = null!;
        public string P256dh { get; set; } = null!;
        public string Auth { get; set; } = null!;
    }

    public class PushAbonelikSilDto
    {
        public string Endpoint { get; set; } = null!;
    }
}
