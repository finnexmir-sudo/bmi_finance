namespace FinNex.Domain.Entities.Risk
{
    /// <summary>
    /// MB Qərar 04/1, Əlavə 4-ün qeydi: "23-26-cı hissələri bankın daxili
    /// qaydaları ilə müəyyən edilmiş limitdən yuxarı həddə olan zərərlər üzrə
    /// doldurulur". Bu hədd qanunda YAZILMIR — bank özü təyin edir (4.1.3-cü
    /// bənd) və dəyişə bilər, ona görə TƏK sətirli parametr cədvəli
    /// (30.09.2026, istifadəçi qərarı: "özünün sazlaya biləcəyi formada
    /// qurmalıyıq") — <see cref="FinNex.Domain.Entities.HR.MezuniyyetHuquqParametrleri"/>
    /// ilə EYNİ naxış.
    ///
    /// ⚠️ Bu, Mərkəzi Banka hesabat həddi (9.9-cu bənd, 100 000 AZN, QANUNLA
    /// SABİT) İLƏ EYNİ ŞEY DEYİL — o hədd kodda sabit qalır, MB dəyişməsə
    /// dəyişməməlidir. Qarışdırma.
    /// </summary>
    public class EmeliyyatRiskiParametrleri : BaseEntity
    {
        /// <summary>Bu məbləğdən (AZN) yuxarı ÜmumiZərər olan hadisələrdə
        /// tədbirlər planı (Əlavə 4, sahə 23-26) məcburi olur.</summary>
        public decimal TedbirZererHeddi { get; set; } = 1000m;

        /// <summary>Sətir tapılmayanda (migration hələ işlədilməyib) işə düşən
        /// default — hesablama heç vaxt boş qalmır.</summary>
        public static EmeliyyatRiskiParametrleri Defolt => new();
    }
}
