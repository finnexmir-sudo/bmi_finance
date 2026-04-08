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
}
