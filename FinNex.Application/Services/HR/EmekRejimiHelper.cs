using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

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

        /// <summary>
        /// Əlil-Cümə qaydası üçün lazım olan datanı TƏK mənbədən yükləyir:
        /// (a) verilən işçilərdən hansıları əlil güzəştlidir, (b) həftəni əhatə edən bayram
        /// (qeyri-iş) günləri. HR, Rəhbər və User davamiyyəti eyni datadan qidalansın deyə buradadır
        /// (3 yerdə ayrıca yazılsa, biri digərindən ayrıla bilər — bu, məhz baş verən problemdir).
        /// Xəta UDULMUR — problem olarsa çağıran tərəf təhlükəsiz davransın (bağışlamadan tutsun).
        /// </summary>
        public static async Task<(HashSet<int> elilIsciIds, HashSet<DateTime> bayramSet)>
            ElilQisaldilmisDataAsync(IUnitOfWork uow, IReadOnlyCollection<int> hedefIsciler,
                                     DateTime minHedef, DateTime maxHedef)
        {
            var elilIsciIds = new HashSet<int>();
            var bayramSet = new HashSet<DateTime>();
            if (hedefIsciler == null || hedefIsciler.Count == 0)
                return (elilIsciIds, bayramSet);

            var isciIdler = hedefIsciler.ToList();
            var ids = await uow.Repository<IsciGuzest>()
                .Query().AsNoTracking()
                .Where(ig => !ig.Silinib && isciIdler.Contains(ig.IsciId) &&
                             ig.Guzest.Ad.Contains("Əlil") &&
                             ig.BaslamaTarixi.Date <= maxHedef &&
                             (ig.BitmeTarixi == null || ig.BitmeTarixi.Value.Date >= minHedef))
                .Select(ig => ig.IsciId)
                .ToListAsync();
            elilIsciIds = new HashSet<int>(ids);

            if (elilIsciIds.Count > 0)
            {
                // Həftə Bazar ertəsindən başlaya bildiyi üçün 7 gün geriyə qədər bayramları çək
                var bayramBas = minHedef.AddDays(-7);
                var bayramlar = await uow.Repository<BayramGunu>()
                    .Query().AsNoTracking()
                    .Where(b => !b.Silinib && b.Tip == GunTipi.Bayram &&
                                b.Tarix.Date >= bayramBas && b.Tarix.Date <= maxHedef)
                    .Select(b => b.Tarix)
                    .ToListAsync();
                bayramSet = new HashSet<DateTime>(bayramlar.Select(d => d.Date));
            }
            return (elilIsciIds, bayramSet);
        }
    }
}
