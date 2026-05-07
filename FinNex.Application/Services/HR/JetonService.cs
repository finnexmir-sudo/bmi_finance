using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.Jeton;
using FinNex.Application.Interfaces.Communication;
using FinNex.Domain;
using FinNex.Domain.Entities.Communication;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.HR
{
    public class JetonService : IJetonService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IBildirisRouter _bildirisRouter;
        private readonly UserManager<AppUser> _userManager;

        public JetonService(
            IUnitOfWork unitOfWork,
            IBildirisRouter bildirisRouter,
            UserManager<AppUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _bildirisRouter = bildirisRouter;
            _userManager = userManager;
        }

        // ── Kataloq ──────────────────────────────────────────────────────────

        public async Task<IList<JetonTeyinatiListDto>> JetonTeyinatlariGetirAsync()
        {
            var list = await _unitOfWork.Repository<JetonTeyinati>()
                .Query()
                .Where(x => x.Aktivdir)
                .OrderBy(x => x.Nov)
                .ThenBy(x => x.Rengi)
                .ToListAsync();

            return list.Select(MapTeyinat).ToList();
        }

        // ── Jeton vermə / ləğvetmə ────────────────────────────────────────────

        public async Task<Result> JetonVerAsync(IsciJetonuCreateDto dto, int verenUserId)
        {
            try
            {
                var isci = await _unitOfWork.Repository<Isci>()
                    .Query().FirstOrDefaultAsync(x => x.Id == dto.IsciId);
                if (isci == null)
                    return Result.Fail("İşçi tapılmadı.");

                var teyinat = await _unitOfWork.Repository<JetonTeyinati>()
                    .Query().FirstOrDefaultAsync(x => x.Id == dto.JetonTeyinatiId && x.Aktivdir);
                if (teyinat == null)
                    return Result.Fail("Jeton növü tapılmadı.");

                var jeton = new IsciJetonu
                {
                    IsciId = dto.IsciId,
                    JetonTeyinatiId = dto.JetonTeyinatiId,
                    Sebeb = dto.Sebeb,
                    VerenUserId = verenUserId,
                    QazanmaTarixi = DateTime.Now,
                    Status = IsciJetonuStatus.Aktiv
                };

                await _unitOfWork.Repository<IsciJetonu>().YaratAsync(jeton);
                await _unitOfWork.YaddaSaxlaAsync();

                // Bildiriş
                var isQara = teyinat.Nov == JetonNovu.Menfi;
                var nov = isQara ? BildirisNovu.QaraJetonVerildi : BildirisNovu.JetonVerildi;
                var bashliq = isQara
                    ? "⛔ Qara Jeton — İntizam xəbərdarlığı"
                    : $"🏅 {teyinat.Ad} qazandınız!";
                var metn = isQara
                    ? $"Sizə intizam pozuntusu üçün Qara Jeton verilib: {dto.Sebeb}. Aktiv Qara jeton olduğu müddətdə jeton xərcləmə imkanınız məhduddur."
                    : $"{teyinat.Ad} ({teyinat.SaatDeyeri} saat) qazandınız. Səbəb: {dto.Sebeb}";

                await _bildirisRouter.NotifyIsciAsync(
                    dto.IsciId, nov, bashliq, metn,
                    redirectUrl: "/User/Jeton/Index");

                return Result.Ok($"{teyinat.Ad} uğurla verildi.");
            }
            catch (Exception ex)
            {
                return Result.Fail($"Xəta: {ex.Message}");
            }
        }

        public async Task<Result> JetonLegvetAsync(int isciJetonuId, string sebeb)
        {
            try
            {
                var jeton = await _unitOfWork.Repository<IsciJetonu>()
                    .Query()
                    .Include(x => x.JetonTeyinati)
                    .FirstOrDefaultAsync(x => x.Id == isciJetonuId);

                if (jeton == null)
                    return Result.Fail("Jeton tapılmadı.");

                if (jeton.Status != IsciJetonuStatus.Aktiv)
                    return Result.Fail("Yalnız aktiv jetonlar ləğv edilə bilər.");

                jeton.Status = IsciJetonuStatus.Legvedildi;
                jeton.Sebeb = jeton.Sebeb + $" [Ləğvetmə: {sebeb}]";

                await _unitOfWork.Repository<IsciJetonu>().YenileAsync(jeton);
                await _unitOfWork.YaddaSaxlaAsync();

                return Result.Ok("Jeton ləğv edildi.");
            }
            catch (Exception ex)
            {
                return Result.Fail($"Xəta: {ex.Message}");
            }
        }

        // ── Sorğular ─────────────────────────────────────────────────────────

        public async Task<IList<IsciJetonuListDto>> IsciAktivJetonlariniGetirAsync(int isciId)
        {
            var list = await _unitOfWork.Repository<IsciJetonu>()
                .Query()
                .Include(x => x.JetonTeyinati)
                .Include(x => x.Isci)
                .Where(x => x.IsciId == isciId && x.Status == IsciJetonuStatus.Aktiv)
                .OrderByDescending(x => x.QazanmaTarixi)
                .ToListAsync();

            return list.Select(MapJeton).ToList();
        }

        public async Task<IList<IsciJetonuListDto>> JetonEmeliyyatlariGetirAsync(int? isciId = null)
        {
            IQueryable<IsciJetonu> query = _unitOfWork.Repository<IsciJetonu>()
                .Query()
                .Include(x => x.JetonTeyinati)
                .Include(x => x.Isci);

            if (isciId.HasValue)
                query = query.Where(x => x.IsciId == isciId.Value);

            var list = await query
                .OrderByDescending(x => x.QazanmaTarixi)
                .ToListAsync();

            return list.Select(MapJeton).ToList();
        }

        public async Task<bool> AktivQaraJetonuVarmiAsync(int isciId)
        {
            return await _unitOfWork.Repository<IsciJetonu>()
                .Query()
                .Include(x => x.JetonTeyinati)
                .AnyAsync(x => x.IsciId == isciId
                    && x.Status == IsciJetonuStatus.Aktiv
                    && x.JetonTeyinati.Nov == JetonNovu.Menfi);
        }

        public async Task<decimal> AktivSaatBalansiAsync(int isciId)
        {
            var jetonlar = await _unitOfWork.Repository<IsciJetonu>()
                .Query()
                .Include(x => x.JetonTeyinati)
                .Where(x => x.IsciId == isciId
                    && x.Status == IsciJetonuStatus.Aktiv
                    && x.JetonTeyinati.Nov == JetonNovu.Musbat)
                .ToListAsync();

            return jetonlar.Sum(x => x.JetonTeyinati.SaatDeyeri);
        }

        // ── Redim ─────────────────────────────────────────────────────────────

        public async Task<Result> RedimTelebiYaratAsync(int isciId, JetonRedimTelebiCreateDto dto)
        {
            try
            {
                if (dto.JetonIds == null || !dto.JetonIds.Any())
                    return Result.Fail("Ən azı bir jeton seçilməlidir.");

                // Qara jeton blok yoxlaması
                var qaraVar = await AktivQaraJetonuVarmiAsync(isciId);
                if (qaraVar)
                    return Result.Fail("Aktiv Qara jetonunuz olduğu üçün jeton xərcləyə bilməzsiniz.");

                // Seçilmiş jetonları yoxla
                var jetonlar = await _unitOfWork.Repository<IsciJetonu>()
                    .Query()
                    .Include(x => x.JetonTeyinati)
                    .Where(x => dto.JetonIds.Contains(x.Id)
                        && x.IsciId == isciId
                        && x.Status == IsciJetonuStatus.Aktiv
                        && x.JetonTeyinati.Nov == JetonNovu.Musbat)
                    .ToListAsync();

                if (jetonlar.Count != dto.JetonIds.Count)
                    return Result.Fail("Seçilmiş jetonların bir hissəsi etibarsızdır.");

                var cemiSaat = jetonlar.Sum(x => x.JetonTeyinati.SaatDeyeri);

                var redim = new JetonRedimTelebi
                {
                    IsciId = isciId,
                    RedimNovu = dto.RedimNovu,
                    CemiSaat = cemiSaat,
                    Status = RedimStatus.Gozlenilir,
                    TelabTarixi = DateTime.Now
                };

                await _unitOfWork.Repository<JetonRedimTelebi>().YaratAsync(redim);
                await _unitOfWork.YaddaSaxlaAsync();

                // Jetonları bu redimə bağla (status dəyişmir — hələ gözlənilir)
                foreach (var j in jetonlar)
                {
                    j.RedimTelebiId = redim.Id;
                    await _unitOfWork.Repository<IsciJetonu>().YenileAsync(j);
                }
                await _unitOfWork.YaddaSaxlaAsync();

                // HR-ə bildiriş
                var redimNovuAd = dto.RedimNovu == RedimNovu.Icaze ? "icazə" : "maaş bonusu";
                await _bildirisRouter.NotifyRolesAsync(
                    new[] { RoleNames.HR, RoleNames.Admin, RoleNames.Rehber },
                    BildirisNovu.JetonVerildi,
                    "Yeni Jeton Redim Sorğusu",
                    $"İşçi {cemiSaat} saatlıq {jetonlar.Count} jetonu {redimNovuAd} kimi xərcləmək istəyir.",
                    redirectUrl: "/HR/Jeton/Index",
                    exceptIsciId: isciId);

                var novLabel = dto.RedimNovu == RedimNovu.Icaze ? "İcazə" : "Maaşa əlavə";
                return Result.Ok($"Redim sorğusu göndərildi. Cəmi: {cemiSaat} saat ({novLabel}).");
            }
            catch (Exception ex)
            {
                return Result.Fail($"Xəta: {ex.Message}");
            }
        }

        public async Task<Result> RedimTelebiTesdiqleAsync(int redimId, int tesdiqleyenUserId)
        {
            try
            {
                var redim = await _unitOfWork.Repository<JetonRedimTelebi>()
                    .Query()
                    .Include(x => x.XerclenenJetonlar)
                    .Include(x => x.Isci)
                    .FirstOrDefaultAsync(x => x.Id == redimId);

                if (redim == null)
                    return Result.Fail("Redim sorğusu tapılmadı.");

                if (redim.Status != RedimStatus.Gozlenilir)
                    return Result.Fail("Bu sorğu artıq emal edilib.");

                redim.Status = RedimStatus.Tesdiqlendi;
                redim.NeticeTarixi = DateTime.Now;
                redim.TesdiqleyenUserId = tesdiqleyenUserId;

                // Jetonları "İstifadə olunub" et
                foreach (var j in redim.XerclenenJetonlar)
                {
                    j.Status = IsciJetonuStatus.IstifadeOlunub;
                    await _unitOfWork.Repository<IsciJetonu>().YenileAsync(j);
                }

                await _unitOfWork.Repository<JetonRedimTelebi>().YenileAsync(redim);
                await _unitOfWork.YaddaSaxlaAsync();

                var novAd = redim.RedimNovu == RedimNovu.Icaze ? "icazə" : "maaş bonusu";
                await _bildirisRouter.NotifyIsciAsync(
                    redim.IsciId,
                    BildirisNovu.JetonRedimTesdiqlendi,
                    "✅ Jeton sorğunuz təsdiqləndi",
                    $"{redim.CemiSaat} saatlıq jeton sorğunuz {novAd} kimi təsdiqləndi.",
                    redirectUrl: "/User/Jeton/Index");

                return Result.Ok("Redim sorğusu təsdiqləndi.");
            }
            catch (Exception ex)
            {
                return Result.Fail($"Xəta: {ex.Message}");
            }
        }

        public async Task<Result> RedimTelebiReddEtAsync(int redimId, string qeyd, int userId)
        {
            try
            {
                var redim = await _unitOfWork.Repository<JetonRedimTelebi>()
                    .Query()
                    .Include(x => x.XerclenenJetonlar)
                    .Include(x => x.Isci)
                    .FirstOrDefaultAsync(x => x.Id == redimId);

                if (redim == null)
                    return Result.Fail("Redim sorğusu tapılmadı.");

                if (redim.Status != RedimStatus.Gozlenilir)
                    return Result.Fail("Bu sorğu artıq emal edilib.");

                redim.Status = RedimStatus.Redd;
                redim.NeticeTarixi = DateTime.Now;
                redim.TesdiqleyenUserId = userId;
                redim.Qeyd = qeyd;

                // Jetonları yenidən aktiv et (sorğu rədd olundu)
                foreach (var j in redim.XerclenenJetonlar)
                {
                    j.RedimTelebiId = null;
                    await _unitOfWork.Repository<IsciJetonu>().YenileAsync(j);
                }

                await _unitOfWork.Repository<JetonRedimTelebi>().YenileAsync(redim);
                await _unitOfWork.YaddaSaxlaAsync();

                await _bildirisRouter.NotifyIsciAsync(
                    redim.IsciId,
                    BildirisNovu.JetonRedimReddEdildi,
                    "❌ Jeton sorğunuz rədd edildi",
                    $"Jeton sorğunuz rədd edildi. Səbəb: {qeyd}",
                    redirectUrl: "/User/Jeton/Index");

                return Result.Ok("Redim sorğusu rədd edildi.");
            }
            catch (Exception ex)
            {
                return Result.Fail($"Xəta: {ex.Message}");
            }
        }

        public async Task<IList<JetonRedimTelebiListDto>> GozleyenRedimlerGetirAsync()
        {
            var list = await _unitOfWork.Repository<JetonRedimTelebi>()
                .Query()
                .Include(x => x.Isci)
                .Include(x => x.XerclenenJetonlar)
                    .ThenInclude(j => j.JetonTeyinati)
                .Where(x => x.Status == RedimStatus.Gozlenilir)
                .OrderBy(x => x.TelabTarixi)
                .ToListAsync();

            return list.Select(MapRedim).ToList();
        }

        public async Task<IList<JetonRedimTelebiListDto>> IsciRedimTarixcesiGetirAsync(int isciId)
        {
            var list = await _unitOfWork.Repository<JetonRedimTelebi>()
                .Query()
                .Include(x => x.Isci)
                .Include(x => x.XerclenenJetonlar)
                    .ThenInclude(j => j.JetonTeyinati)
                .Where(x => x.IsciId == isciId)
                .OrderByDescending(x => x.TelabTarixi)
                .ToListAsync();

            return list.Select(MapRedim).ToList();
        }

        // ── Köməkçi mapper metodlar ───────────────────────────────────────────

        private static JetonTeyinatiListDto MapTeyinat(JetonTeyinati x) => new()
        {
            Id = x.Id,
            Ad = x.Ad,
            Nov = x.Nov,
            Rengi = x.Rengi,
            SaatDeyeri = x.SaatDeyeri,
            Ikon = x.Ikon,
            RengKodu = x.RengKodu,
            Tesvir = x.Tesvir,
            Aktivdir = x.Aktivdir
        };

        private static IsciJetonuListDto MapJeton(IsciJetonu x) => new()
        {
            Id = x.Id,
            IsciId = x.IsciId,
            IsciTamAd = $"{x.Isci.Ad} {x.Isci.Soyad}".Trim(),
            JetonTeyinatiId = x.JetonTeyinatiId,
            JetonAd = x.JetonTeyinati.Ad,
            JetonNovu = x.JetonTeyinati.Nov,
            JetonRengi = x.JetonTeyinati.Rengi,
            JetonIkon = x.JetonTeyinati.Ikon,
            JetonRengKodu = x.JetonTeyinati.RengKodu,
            JetonSaatDeyeri = x.JetonTeyinati.SaatDeyeri,
            QazanmaTarixi = x.QazanmaTarixi,
            Sebeb = x.Sebeb,
            Status = x.Status,
            RedimTelebiId = x.RedimTelebiId
        };

        private static JetonRedimTelebiListDto MapRedim(JetonRedimTelebi x) => new()
        {
            Id = x.Id,
            IsciId = x.IsciId,
            IsciTamAd = $"{x.Isci.Ad} {x.Isci.Soyad}".Trim(),
            RedimNovu = x.RedimNovu,
            CemiSaat = x.CemiSaat,
            Status = x.Status,
            TelabTarixi = x.TelabTarixi,
            NeticeTarixi = x.NeticeTarixi,
            Qeyd = x.Qeyd,
            XerclenenJetonlar = x.XerclenenJetonlar.Select(MapJeton).ToList()
        };
    }
}
