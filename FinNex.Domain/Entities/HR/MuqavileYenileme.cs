namespace FinNex.Domain.Entities.HR
{
    public class MuqavileYenileme : BaseEntity
    {
        public int       IsciId            { get; set; }
        public Isci?     Isci              { get; set; }

        public DateTime? KohneBitmeTarixi  { get; set; }  // əvvəlki bitmə tarixi
        public DateTime  YeniBitmeTarixi   { get; set; }  // yeni bitmə tarixi
        public DateTime  YenilemeTarixi    { get; set; } = DateTime.Now;
        public string?   Qeyd              { get; set; }
    }
}
