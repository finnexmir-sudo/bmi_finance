namespace FinNex.Domain.Entities.HR
{
    /// <summary>
    /// İstifadə edilməmiş əmək məzuniyyəti günlərinə görə kompensasiya.
    /// İşçi ay ərzində işdən çıxdıqda HR bu səhifədə hesablama aparır,
    /// nəticə yadda saxlanılır və həmin ayın maaş hesablamasında gəlir
    /// kimi avtomatik daxil olur.
    ///
    /// Hesablama məntiqi:
    ///   • Keçmiş illər: MezuniyyetBalans-da bütün il < ayrılma ili
    ///     üçün qalıq (ToplamGun − IstifadeOlunanGun) cəmi.
    ///   • Cari il prorate: sonuncu məzuniyyətin BitmeTarixi-dən (yoxdursa
    ///     IsheQebulTarixi) ayrılma tarixinə qədər keçən gün / 365
    ///     × illik gün hüququ − cari ildə artıq istifadə edilən günlər.
    ///   • Günlük rate: məzuniyyət haqqı düsturu (MAX(S/12/30.4, Maas/ayIsGun))
    ///     ilə eyni — son 12 ay artım əmsallı qazanc cəmi əsasında.
    /// </summary>
    public class MezuniyyetKompensasiyasi : BaseEntity
    {
        public int IsciId { get; set; }
        public Isci Isci { get; set; } = null!;

        public DateTime AyrilmaTarixi { get; set; }

        // Sonuncu məzuniyyət (prorate üçün) — yoxdursa null, prorate
        // IsheQebulTarixi-dən hesablanır.
        public DateTime? SonuncuMezuniyyetBitmeTarixi { get; set; }
        public int? SonuncuMezuniyyetId { get; set; }

        // Sonuncu məzuniyyət bitişindən ayrılma tarixinə qədər keçən gün
        public int KecenGunSayi { get; set; }

        // Gün hesablanması (ondalıklı — prorate üçün)
        public decimal KecmisQaligGun { get; set; }
        public decimal CariIlProrateGun { get; set; }
        public decimal CemiKompensasiyaGun { get; set; }

        // Günlük rate hesablaması — şəffaflıq üçün
        public decimal Son12AyDuzelmisQazanc { get; set; }   // S
        public decimal GunlukMezPul { get; set; }            // S / 12 / 30.4
        public decimal GunlukMaas { get; set; }              // CariMaas / 22 (təxminən)
        public decimal GunlukRate { get; set; }              // MAX-ın seçdiyi

        public decimal CemiMebleg { get; set; }

        // Maaşa daxil etmək üçün hədəf dövr
        public int HesablananIl { get; set; }
        public int HesablananAy { get; set; }
        public int? MaasId { get; set; }                     // hansı Maas qeydinə yapışdırılıb

        public KompensasiyaStatus Status { get; set; } = KompensasiyaStatus.Layihe;
        public string? Qeyd { get; set; }
        public int HesablayanIsciId { get; set; }
    }

    public enum KompensasiyaStatus
    {
        Layihe = 1,
        Tesdiqlenib = 2,
        MaasaDaxilEdildi = 3,
        LegvEdildi = 99
    }
}
