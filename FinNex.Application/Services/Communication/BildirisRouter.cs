using FinNex.Application.Interfaces.Communication;
using FinNex.Domain;
using FinNex.Domain.Entities.Communication;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace FinNex.Application.Services.Communication
{
    public class BildirisRouter : IBildirisRouter
    {
        private readonly IBildirisService _bildirisService;
        private readonly UserManager<AppUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;

        public BildirisRouter(
            IBildirisService bildirisService,
            UserManager<AppUser> userManager,
            IUnitOfWork unitOfWork)
        {
            _bildirisService = bildirisService;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
        }

        public async Task NotifyIsciAsync(
            int isciId,
            BildirisNovu nov,
            string bashliq,
            string metn,
            string? redirectUrl = null,
            int? mezuniyyetId = null,
            int? icazeId = null)
        {
            try
            {
                if (isciId <= 0) return;

                await _bildirisService.YaratAsync(
                    isciId: isciId,
                    nov: nov,
                    bashliq: bashliq,
                    metn: metn,
                    redirectUrl: redirectUrl,
                    mezuniyyetId: mezuniyyetId,
                    icazeId: icazeId);
            }
            catch
            {
                // Bildiriş xətası əsas əməliyyatı pozmasın.
            }
        }

        public Task NotifyRoleAsync(
            string roleName,
            BildirisNovu nov,
            string bashliq,
            string metn,
            string? redirectUrl = null,
            int? mezuniyyetId = null,
            int? icazeId = null,
            int? exceptIsciId = null)
            => NotifyRolesAsync(new[] { roleName }, nov, bashliq, metn,
                redirectUrl, mezuniyyetId, icazeId, exceptIsciId);

        public async Task NotifyRolesAsync(
            IEnumerable<string> roleNames,
            BildirisNovu nov,
            string bashliq,
            string metn,
            string? redirectUrl = null,
            int? mezuniyyetId = null,
            int? icazeId = null,
            int? exceptIsciId = null)
        {
            try
            {
                var alici = new HashSet<int>();
                foreach (var rol in roleNames.Where(r => !string.IsNullOrWhiteSpace(r)).Distinct())
                {
                    var users = await _userManager.GetUsersInRoleAsync(rol);
                    foreach (var u in users)
                    {
                        if (!u.IsciId.HasValue) continue;
                        if (exceptIsciId.HasValue && u.IsciId.Value == exceptIsciId.Value) continue;
                        alici.Add(u.IsciId.Value);
                    }
                }

                foreach (var isciId in alici)
                {
                    await _bildirisService.YaratAsync(
                        isciId: isciId,
                        nov: nov,
                        bashliq: bashliq,
                        metn: metn,
                        redirectUrl: redirectUrl,
                        mezuniyyetId: mezuniyyetId,
                        icazeId: icazeId);
                }
            }
            catch
            {
                // Bildiriş xətası əsas əməliyyatı pozmasın.
            }
        }

        public async Task NotifyStrukturRoluAsync(
            StrukturRolTipi rolTipi,
            BildirisNovu nov,
            string bashliq,
            string metn,
            string? redirectUrl = null,
            int? mezuniyyetId = null,
            int? icazeId = null,
            int? exceptIsciId = null)
        {
            try
            {
                var rollar = await _unitOfWork.Repository<IsciStrukturRolu>()
                    .HamisiniGetirAsync(
                        predicate: x => x.Aktivdir
                                       && x.RolTipi == rolTipi
                                       && (x.BitmeTarixi == null || x.BitmeTarixi >= DateTime.Now),
                        izlemeden: true);

                foreach (var r in rollar)
                {
                    if (exceptIsciId.HasValue && r.IsciId == exceptIsciId.Value) continue;

                    await _bildirisService.YaratAsync(
                        isciId: r.IsciId,
                        nov: nov,
                        bashliq: bashliq,
                        metn: metn,
                        redirectUrl: redirectUrl,
                        mezuniyyetId: mezuniyyetId,
                        icazeId: icazeId);
                }
            }
            catch
            {
                // Bildiriş xətası əsas əməliyyatı pozmasın.
            }
        }

        public async Task NotifyDepartmentRoleAsync(
            int departamentId,
            StrukturRolTipi rolTipi,
            BildirisNovu nov,
            string bashliq,
            string metn,
            string? redirectUrl = null,
            int? mezuniyyetId = null,
            int? icazeId = null,
            int? exceptIsciId = null)
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

                foreach (var r in rollar)
                {
                    if (exceptIsciId.HasValue && r.IsciId == exceptIsciId.Value) continue;

                    await _bildirisService.YaratAsync(
                        isciId: r.IsciId,
                        nov: nov,
                        bashliq: bashliq,
                        metn: metn,
                        redirectUrl: redirectUrl,
                        mezuniyyetId: mezuniyyetId,
                        icazeId: icazeId);
                }
            }
            catch
            {
                // Bildiriş xətası əsas əməliyyatı pozmasın.
            }
        }
    }
}
