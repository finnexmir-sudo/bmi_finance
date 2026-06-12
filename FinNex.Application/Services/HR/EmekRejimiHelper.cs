namespace FinNex.Application.Services.HR
{
    /// <summary>
    /// Əlil işçilərin iş rejimi ilə bağlı paylaşılan hesablama köməkçiləri.
    /// Davamiyyət ("tez çıxan") və Tabel EYNİ qaydanı tətbiq etsin deyə tək yerdə saxlanılır
    /// (iki yerdə ayrıca yazılsa, vaxt keçdikcə bir-birindən ayrılma riski var).
    /// </summary>
    public static class EmekRejimiHelper
    {
        /// <summary>
        /// Əlil işçi üçün həmin günün qısaldılmış (4 saat) iş günü olub-olmadığını qaytarır.
        /// Qayda — TabelService ilə eynidir: TAM 5 günlük iş həftəsində (Be–Cü, heç biri bayram deyil)
        /// 5-ci iş günü = Cümə → 4 saat. Həftədə bayram varsa (natamam həftə) qısaltma tətbiq olunmur,
        /// həftə sonu (Şənbə/Bazar) isə heç vaxt qısaldılmış gün deyil.
        /// </summary>
        /// <param name="gun">Yoxlanılan gün.</param>
        /// <param name="bayramGunleri">Həmin həftəni əhatə edən bayram (istirahət) günləri dəsti.</param>
        public static bool ElilQisaldilmisGun(DateTime gun, ISet<DateTime> bayramGunleri)
        {
            // Bazar=7, qalan günlər DayOfWeek dəyəri (Be=1 … Şə=6)
            int dow = gun.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)gun.DayOfWeek;
            if (dow > 5) return false; // həftə sonu — qısaldılmış gün ola bilməz

            var bazarErtesi = gun.Date.AddDays(-(dow - 1)); // həmin həftənin Bazar ertəsi
            int isGunSayi = 0, gunIndeksi = -1, idx = 0;
            for (int wd = 0; wd < 5; wd++) // yalnız Be–Cü nəzərə alınır
            {
                var w = bazarErtesi.AddDays(wd);
                if (bayramGunleri.Contains(w.Date)) continue; // bayram → iş günü sayılmır
                if (w.Date == gun.Date) gunIndeksi = idx;
                isGunSayi++;
                idx++;
            }
            // Tam 5 günlük həftə VƏ bu gün 5-ci iş günü (Cümə)
            return isGunSayi == 5 && gunIndeksi == 4;
        }
    }
}
