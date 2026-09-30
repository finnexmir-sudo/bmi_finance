namespace FinNex.Domain.Entities.Risk
{
    /// <summary>
    /// Bir <see cref="EmeliyyatRiskiHadisesi"/> qeydinin HƏR redaktəsindən
    /// ƏVVƏLKİ tam vəziyyətini saxlayır (30.09.2026, istifadəçi qərarı:
    /// "saxlanılsın" — redaktə tarixçəsi itməməlidir).
    ///
    /// Naxış layihədə artıq mövcuddur — `Isci.EvvelkiStajPeriodlari` kimi
    /// JSON-da struktur saxlamaq; fərq: burada JSON snapshot AYRI cədvəldədir
    /// (hər redaktə üçün bir sətir), çünki tarixçə sayı əvvəlcədən bilinmir
    /// və əsas cədvəli şişirtmək istəmirik. 27 sütunu güzgü kimi TƏKRARLAMIRIQ
    /// — `EvvelkiDeyerlerJson` bütöv snapshotdur, oxumaq üçün deserialize edilir.
    /// </summary>
    public class EmeliyyatRiskiHadisesiTarixce : BaseEntity
    {
        public int HadiseId { get; set; }

        /// <summary>İstifadəçinin redaktə anında yazdığı qısa izah — Əlavə 4,
        /// sahə 27 ("dəyişikliyin qısa təsviri") burada MƏCBURİ doldurulur.</summary>
        public string DeyisiklikTesviri { get; set; } = "";

        /// <summary>Redaktədən ƏVVƏLKİ tam qeyd — JSON (bax
        /// `EmeliyyatRiskiHadisesiService.Snapshot`). Nöqtə-zamanlı vəziyyəti
        /// bərpa etmək üçün kifayətdir.</summary>
        public string EvvelkiDeyerlerJson { get; set; } = "";
    }
}
