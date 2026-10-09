namespace FinNex.Domain.Entities.Kassa
{
    /// <summary>
    /// Bir valyutanın (USD/AVRO/IRR/AED/RUB) nağd və qeyri-nağd alış-satış
    /// məzənnəsi — <see cref="KassaKursBeyannamesi"/>-yə bağlıdır.
    /// </summary>
    public class KassaKursSetri : BaseEntity
    {
        public int BeyannameId { get; set; }
        public KassaKursBeyannamesi Beyanname { get; set; } = null!;

        public string Valyuta { get; set; } = null!;   // "USD" / "AVRO" / "IRR" / "AED" / "RUB"

        public decimal? NagdAlis { get; set; }
        public decimal? NagdSatis { get; set; }
        public decimal? QeyriNagdAlis { get; set; }
        public decimal? QeyriNagdSatis { get; set; }
    }
}
