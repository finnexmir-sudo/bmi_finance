namespace FinNex.UI.Areas.SenedDovriyyesi.ViewModels
{
    public class TopluStatusDeyisRequest
    {
        public List<int> Ids { get; set; } = new();
        public int NewStatus { get; set; }
    }

    public class TopluSilRequest
    {
        public List<int> Ids { get; set; } = new();
    }

    public class EtiketYaratRequest
    {
        public string Ad { get; set; } = null!;
    }

    public class SenedNovuRedakteRequest
    {
        public int Id { get; set; }
        public string Ad { get; set; } = null!;
    }
}
