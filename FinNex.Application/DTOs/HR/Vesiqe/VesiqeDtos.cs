namespace FinNex.Application.DTOs.HR.Vesiqe
{
    /// <summary>
    /// Bir işçinin şəxsiyyət vəsiqəsi məlumatı.
    ///
    /// ⚠️ BU MƏLUMAT HEÇ YERƏ YAZILMIR (istifadəçi qərarı 09.09.2026) —
    /// tarix hər dəfə BMI-dən (Oracle) canlı oxunur. `Isci` cədvəlində
    /// belə bir sütun YOXDUR və olmamalıdır: iki nüsxə saxlansa biri
    /// gec-tez köhnələr və hansının doğru olduğu bilinməzdi.
    /// </summary>
    public class VesiqeSetriDto
    {
        public int     IsciId        { get; set; }
        public string  Ad            { get; set; } = "";
        public string  Soyad         { get; set; } = "";
        public string? AtaAdi        { get; set; }
        public string  FIN           { get; set; } = "";
        public string  DepartamentAd { get; set; } = "—";
        public string  VezifeAd      { get; set; } = "—";
        public int     DepartamentId { get; set; }

        /// <summary>BMI `regnom.pasport_date_close`. NULL = BMI-də tapılmadı/boşdur.</summary>
        public DateTime? BitmeTarixi { get; set; }

        /// <summary>Bu günə qədər qalan gün. Tarix yoxdursa NULL.</summary>
        public int? QalanGun { get; set; }

        public string TamAd => $"{Ad} {Soyad} {AtaAdi}".Trim();

        /// <summary>«yoxdur» / «kecmis» / «qirmizi» / «sari» / «normal»</summary>
        public string Tecililik =>
            QalanGun == null   ? "yoxdur"  :
            QalanGun <  0      ? "kecmis"  :
            QalanGun <= 30     ? "qirmizi" :
            QalanGun <= 90     ? "sari"    : "normal";
    }

    public class VesiqeNeticesi
    {
        /// <summary>BMI-dən oxuma alındımı. `false` olduqda `Xeta` doludur.</summary>
        public bool    Ugurlu { get; set; }
        public string? Xeta   { get; set; }

        public List<VesiqeSetriDto> Setirler { get; set; } = new();
    }
}
