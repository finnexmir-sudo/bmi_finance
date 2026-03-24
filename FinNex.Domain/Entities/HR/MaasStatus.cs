namespace FinNex.Domain.Entities.HR
{
    public enum MaasStatus
    {
        // 1. Maaş hələ hesablanır, üzərində düzəlişlər edilə bilər.
        Layihe = 1,

        // 2. Hesablanıb bitib, mühasibatlıq və ya rəhbərlik tərəfindən yoxlanılıb təsdiq edilib.
        // Təsdiqlənmiş maaş üzərində dəyişiklik etmək olmaz.
        Tesdiqlendi = 2,

        // 3. Maaş faylı banka göndərilib və ya işçilərin kartına köçürülüb.
        Odenildi = 3,

        // 4. Əgər hesablamada ciddi bir səhv tapılıbsa və ya işçi ilə bağlı problem varsa,
        // həmin ayın maaşı ləğv edilə bilər.
        LegvEdildi = 4
    }
}