namespace FinNex.Application.DTOs.Kassa
{
    /// <summary>Kassirin daxil etdiyi günlük 5 valyutalıq dəst (yazma).</summary>
    public class KassaKursSaxlaDto
    {
        public DateTime Tarix { get; set; }
        public List<KassaKursSetriDto> Setirler { get; set; } = new();
    }
}
