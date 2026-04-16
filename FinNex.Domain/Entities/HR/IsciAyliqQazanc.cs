namespace FinNex.Domain.Entities.HR
{
    /// <summary>
    /// İşçinin aylıq məzuniyyət-əsaslı qazancı.
    ///
    /// 2026 Əmək Məcəlləsinin 140-cı maddəsinə uyğun məzuniyyət ödənişinin
    /// hesablanması üçün son 12 ayın qazancları saxlanılır.
    ///
    /// Formula:
    ///   S    = Son 12 ayın qazancı (xəstəlik çıxılıb)
    ///   MH   = S / 12 / 30.4 × GS    (təqvim günü)
    ///   ƏH   = CariMaas / AyIşGün × İGS  (iş günü)
    ///   Ödəniş = MAX(MH, ƏH)
    ///
    /// Sliding window: hər işçi üçün maksimum 12 qeyd saxlanılır.
    /// Yeni qeyd əlavə olunduqda ən köhnə 13-cü qeyd avtomatik silinir.
    /// </summary>
    public class IsciAyliqQazanc : BaseEntity
    {
        public int IsciId { get; set; }
        public Isci Isci { get; set; } = null!;

        public int Il { get; set; }
        public int Ay { get; set; }

        /// <summary>
        /// Məzuniyyət üçün əsas alınan aylıq qazanc.
        /// ƏHƏMİYYƏTLİ: xəstəlik ödənişi artıq çıxılmış olmalıdır.
        /// </summary>
        public decimal Qazanc { get; set; }

        /// <summary>
        /// true = HR əl ilə daxil edib (tarixi məlumat)
        /// false = sistem avtomatik əlavə edib (yeni hesablama nəticəsi)
        /// </summary>
        public bool ElIleDaxilEdilib { get; set; }

        public string? Qeyd { get; set; }
    }
}
