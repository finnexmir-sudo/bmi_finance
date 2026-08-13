using FinNex.Application.Interfaces.Communication;
using FinNex.Domain;
using FinNex.Domain.Entities.Communication;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace FinNex.Application.Services.Communication
{
    public class BildirisRouter : IBildirisRouter
    {
        private readonly IBildirisService      _bildirisService;
        private readonly UserManager<AppUser>  _userManager;
        private readonly IUnitOfWork           _unitOfWork;
        private readonly ILogger<BildirisRouter> _logger;

        public BildirisRouter(
            IBildirisService        bildirisService,
            UserManager<AppUser>    userManager,
            IUnitOfWork             unitOfWork,
            ILogger<BildirisRouter> logger)
        {
            _bildirisService = bildirisService;
            _userManager     = userManager;
            _unitOfWork      = unitOfWork;
            _logger          = logger;
        }

        // ── Bildirişlər NİYƏ ARDICIL YAZILIR (KRİTİK) ──────────────────────
        // Əvvəl burada `Task.WhenAll(...)` var idi — bütün alıcılara PARALEL yazırdı.
        // `BildirisService` isə sorğunun ORTAQ `IUnitOfWork`-unu (yəni eyni
        // `DbContext`-i) işlədir; EF Core-un `DbContext`-i thread-safe DEYİL.
        // Nəticə: eyni kontekstdə paralel `Add` + `SaveChanges` ya istisna verirdi
        // (və aşağıdakı boş `catch` onu udurdu → bildiriş səssizcə itirdi), ya da
        // sətri TƏKRAR yazırdı.
        //
        // Real hadisə (13.08.2026): bir məzuniyyət müraciəti üçün rəhbərə iki eyni
        // bildiriş düşdü — 3,3 ms fərqlə; başqa iki sətrin `YaradilmaTarixi`-si isə
        // tick-tick eyni idi, yəni həqiqətən paralel yazılmışdılar.
        //
        // Bildiriş sayı azdır (bir neçə alıcı), paralellikdən qazanc yoxdur —
        // itki və dublikat isə realdır. Ona görə HƏR YERDƏ ardıcıl `await`.
        // Yeni toplu bildiriş metodu yazsan, `Task.WhenAll` İSTİFADƏ ETMƏ.
        private async Task GonderAsync(
            IEnumerable<int> aliciIsciIdler,
            BildirisNovu nov,
            string bashliq,
            string metn,
            string? redirectUrl,
            int? mezuniyyetId,
            int? icazeId)
        {
            foreach (var isciId in aliciIsciIdler)
            {
                var netice = await _bildirisService.YaratAsync(
                    isciId:       isciId,
                    nov:          nov,
                    bashliq:      bashliq,
                    metn:         metn,
                    redirectUrl:  redirectUrl,
                    mezuniyyetId: mezuniyyetId,
                    icazeId:      icazeId);

                // Bildiriş xətası əsas əməliyyatı pozmur, amma İZSİZ də qalmır.
                if (!netice.Success)
                    _logger.LogWarning(
                        "Bildiriş yazılmadı — isciId={IsciId}, nov={Nov}, mezuniyyetId={MezuniyyetId}, icazeId={IcazeId}: {Sebeb}",
                        isciId, nov, mezuniyyetId, icazeId, netice.Message);
            }
        }

        public async Task NotifyIsciAsync(
            int isciId,
            BildirisNovu nov,
            string bashliq,
            string metn,
            string? redirectUrl  = null,
            int?   mezuniyyetId  = null,
            int?   icazeId       = null)
        {
            try
            {
                if (isciId <= 0) return;

                await GonderAsync(new[] { isciId }, nov, bashliq, metn,
                    redirectUrl, mezuniyyetId, icazeId);
            }
            catch (Exception ex)
            {
                // Bildiriş xətası əsas əməliyyatı POZMUR, amma izsiz qalmır.
                _logger.LogError(ex, "NotifyIsciAsync xətası — isciId={IsciId}, nov={Nov}", isciId, nov);
            }
        }

        public Task NotifyRoleAsync(
            string roleName,
            BildirisNovu nov,
            string bashliq,
            string metn,
            string? redirectUrl  = null,
            int?   mezuniyyetId  = null,
            int?   icazeId       = null,
            int?   exceptIsciId  = null)
            => NotifyRolesAsync(new[] { roleName }, nov, bashliq, metn,
                redirectUrl, mezuniyyetId, icazeId, exceptIsciId);

        public async Task NotifyRolesAsync(
            IEnumerable<string> roleNames,
            BildirisNovu nov,
            string bashliq,
            string metn,
            string? redirectUrl  = null,
            int?   mezuniyyetId  = null,
            int?   icazeId       = null,
            int?   exceptIsciId  = null)
        {
            try
            {
                var alici = new HashSet<int>();
                foreach (var rol in roleNames
                    .Where(r => !string.IsNullOrWhiteSpace(r))
                    .Distinct())
                {
                    var users = await _userManager.GetUsersInRoleAsync(rol);
                    foreach (var u in users)
                    {
                        if (!u.IsciId.HasValue) continue;
                        if (exceptIsciId.HasValue && u.IsciId.Value == exceptIsciId.Value) continue;
                        alici.Add(u.IsciId.Value);
                    }
                }

                await GonderAsync(alici, nov, bashliq, metn,
                    redirectUrl, mezuniyyetId, icazeId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NotifyRolesAsync xətası — nov={Nov}, mezuniyyetId={MezuniyyetId}",
                    nov, mezuniyyetId);
            }
        }

        public async Task NotifyStrukturRoluAsync(
            StrukturRolTipi rolTipi,
            BildirisNovu nov,
            string bashliq,
            string metn,
            string? redirectUrl  = null,
            int?   mezuniyyetId  = null,
            int?   icazeId       = null,
            int?   exceptIsciId  = null)
        {
            try
            {
                var rollar = await _unitOfWork.Repository<IsciStrukturRolu>()
                    .HamisiniGetirAsync(
                        predicate: x => x.Aktivdir
                                     && x.RolTipi == rolTipi
                                     && (x.BitmeTarixi == null || x.BitmeTarixi >= DateTime.Now),
                        izlemeden: true);

                var hedefler = rollar
                    .Where(r => !exceptIsciId.HasValue || r.IsciId != exceptIsciId.Value)
                    .ToList();

                // Eyni işçinin bir neçə aktiv struktur rolu ola bilər — Distinct
                // olmasa eyni adama eyni bildiriş bir neçə dəfə gedərdi.
                await GonderAsync(hedefler.Select(r => r.IsciId).Distinct(),
                    nov, bashliq, metn, redirectUrl, mezuniyyetId, icazeId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Struktur rolu bildirişi xətası — rolTipi={RolTipi}, nov={Nov}",
                    rolTipi, nov);
            }
        }

        public async Task NotifyDepartmentRoleAsync(
            int departamentId,
            StrukturRolTipi rolTipi,
            BildirisNovu nov,
            string bashliq,
            string metn,
            string? redirectUrl  = null,
            int?   mezuniyyetId  = null,
            int?   icazeId       = null,
            int?   exceptIsciId  = null)
        {
            try
            {
                if (departamentId <= 0) return;

                var rollar = await _unitOfWork.Repository<IsciStrukturRolu>()
                    .HamisiniGetirAsync(
                        predicate: x => x.Aktivdir
                                     && x.DepartamentId == departamentId
                                     && x.RolTipi == rolTipi
                                     && (x.BitmeTarixi == null || x.BitmeTarixi >= DateTime.Now),
                        izlemeden: true);

                var hedefler = rollar
                    .Where(r => !exceptIsciId.HasValue || r.IsciId != exceptIsciId.Value)
                    .ToList();

                // Eyni işçinin bir neçə aktiv struktur rolu ola bilər — Distinct
                // olmasa eyni adama eyni bildiriş bir neçə dəfə gedərdi.
                await GonderAsync(hedefler.Select(r => r.IsciId).Distinct(),
                    nov, bashliq, metn, redirectUrl, mezuniyyetId, icazeId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Struktur rolu bildirişi xətası — rolTipi={RolTipi}, nov={Nov}",
                    rolTipi, nov);
            }
        }
    }
}
