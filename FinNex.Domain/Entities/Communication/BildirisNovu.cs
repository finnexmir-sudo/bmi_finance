// FinNex.Domain.Entities.Communication/BildirisNovu.cs
// FinNex.Domain.Entities.Communication/Bildiris.cs
namespace FinNex.Domain.Entities.Communication
{
    public enum BildirisNovu
    {
        MezuniyyetMuraciet = 1,
        MezuniyyetTesdiq = 2,
        MezuniyyetImtina = 3,
        EvezediciSorgu = 4,
        EvezediciQebul = 5,
        EvezediciRedd = 6,
        YeniMesaj = 7,
        IcazeMuraciet = 8,
        IcazeTesdiq = 9,
        IcazeImtina = 10,
        TesdiqSorgusu = 11,
        YeniTapshiriq = 12,
        TapshiriqSonTarixYaxinlashir = 13,
        TapshiriqGecikdi = 14,
        TapshiriqTamamlandi = 15,
        YeniGorush = 16,
        GorushXatirlama = 17,
        GorushLegv = 18,
        MuqavileYenilenme = 19,

        // Avans iş axını
        AvansMuraciet = 20,
        AvansTesdiq = 21,
        AvansImtina = 22,

        // Xərc iş axını
        XercMuraciet = 23,
        XercTesdiq = 24,
        XercImtina = 25,
        XercOdenis = 26,

        // Maaş iş axını (işçiyə bildiriş)
        MaasOdenildi = 27,
        MaasReddedildi = 28,

        // Məzuniyyət ödənişi (mühasibə ay sonu axını)
        MezuniyyetOdenisGozleyir = 29,

        // Məzuniyyət ödənişi işçiyə — planlı / icra edildi
        MezuniyyetOdenisPlanlandi = 30,
        MezuniyyetOdenisIcraEdildi = 31,

        // Jeton (Gamification) bildirişləri
        JetonVerildi = 32,
        QaraJetonVerildi = 33,
        JetonRedimTesdiqlendi = 34,
        JetonRedimReddEdildi = 35,

        // Teklif / Boşluq / Şikayət iş axını
        TeklifGonderildi = 36,
        TeklifCavab = 37,

        // Gələn Mail
        YeniGelenMail = 38
    }
}
