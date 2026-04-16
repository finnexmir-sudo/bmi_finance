namespace FinNex.Domain.Entities.HR
{
    /// <summary>
    /// Xəstəlik ödənişinin audit qeydi.
    ///
    /// Hər xəstəlik üçün hər ay üzrə bir qeyd yaradılır
    /// (xəstəlik 2 aya bölünərsə 2 qeyd olur).
    ///
    /// Hesablama:
    ///   BirGunluk = S / SonIlIsGunu
    ///   SirketOdenis = BirGunluk × SirketGunSayi (max 14 iş günü)
    ///   DsmfOdenis   = BirGunluk × DsmfGunSayi   (yalnız informativ)
    /// </summary>
    public class XestelikOdenis : BaseEntity
    {
        public int XestelikId { get; set; }
        public Xestelik Xestelik { get; set; } = null!;

        public int IsciId { get; set; }
        public Isci Isci { get; set; } = null!;

        /// <summary>Hansı ayın maaşına əlavə olundu</summary>
        public int Il { get; set; }
        public int Ay { get; set; }

        /// <summary>S / Son12AyIsGunu — bir günlük dərəcə</summary>
        public decimal BirGunluk { get; set; }

        /// <summary>Bu ayda şirkətin ödədiyi günlər (max 14, ümumi limit)</summary>
        public int SirketGunSayi { get; set; }

        /// <summary>Bu ayda DSMF-in ödəyəcəyi günlər (informativ)</summary>
        public int DsmfGunSayi { get; set; }

        /// <summary>Şirkət ödənişi (Brüt-ə əlavə olunur)</summary>
        public decimal SirketOdenis { get; set; }

        /// <summary>DSMF ödənişi (yalnız informativ)</summary>
        public decimal DsmfOdenis { get; set; }

        /// <summary>Hansı maaşa bağlandı (audit)</summary>
        public int? MaasId { get; set; }
        public Maas? Maas { get; set; }
    }
}
