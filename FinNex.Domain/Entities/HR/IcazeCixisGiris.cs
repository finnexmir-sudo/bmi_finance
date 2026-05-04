namespace FinNex.Domain.Entities.HR
{
    public class IcazeCixisGiris : BaseEntity
    {
        public int IcazeId { get; set; }
        public Icaze Icaze { get; set; } = null!;

        // Fiziki çıxış/qayıdış vaxtları (cihazdan gəlir)
        public DateTime? CixisVaxt { get; set; }
        public DateTime? QayidisVaxt { get; set; }

        // Birdefelik çıxış: qayıdış gözlənilmir (HR təsdiq zamanı işarələyir)
        public bool Birdefelik { get; set; } = false;

        // Faktiki müddət = QayidisVaxt - CixisVaxt (Birdefelik isə hesablanmır)
        public double? FaktikiSaat =>
            (!Birdefelik && CixisVaxt.HasValue && QayidisVaxt.HasValue)
                ? (QayidisVaxt.Value - CixisVaxt.Value).TotalHours
                : null;

        public IcazeCixisGirisStatus Status { get; set; } = IcazeCixisGirisStatus.Gozlenir;
    }

    public enum IcazeCixisGirisStatus
    {
        Gozlenir   = 1,  // Hələ çıxmayıb
        Cixdi      = 2,  // Çıxdı, qayıdış gözlənilir
        Tamamlandi = 3,  // Qayıdış qeyd olundu / birdefelik tamamlandı
        LegvEdildi = 4   // İcazə ləğv edildi
    }
}
