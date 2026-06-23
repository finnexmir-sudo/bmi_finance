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
        private readonly IReytingService _reytingService;

        public JetonService(
            IUnitOfWork unitOfWork,
            IBildirisRouter bildirisRouter,
            UserManager<AppUser> userManager,
            IReytingService reytingService)
        {
            _unitOfWork = unitOfWork;
            _bildirisRouter = bildirisRouter;
            _userManager = userManager;
            _reytingService = reytingService;
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

                var eded = dto.Eded < 1 ? 1 : (dto.Eded > 50 ? 50 : dto.Eded);

                // Qara jeton üçün eded həmişə 1 — intizam xəbərdarlığı dublicate edilməsin
                if (teyinat.Nov == JetonNovu.Menfi) eded = 1;

                for (int i = 0; i < eded; i++)
                {
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
                }
                await _unitOfWork.YaddaSaxlaAsync();

                // Bildiriş — bir bildiriş çoxsaylı jetonu əhatə edir
                var isQara = teyinat.Nov == JetonNovu.Menfi;
                var nov = isQara ? BildirisNovu.QaraJetonVerildi : BildirisNovu.JetonVerildi;
                var edSuffix = eded > 1 ? $" × {eded}" : "";
                var bashliq = isQara
                    ? "⛔ Qara Jeton — İntizam xəbərdarlığı"
                    : $"🏅 {teyinat.Ad}{edSuffix} qazandınız!";
                var metn = isQara
                    ? $"Sizə intizam pozuntusu üçün Qara Jeton verilib: {dto.Sebeb}. Aktiv Qara jeton olduğu müddətdə jeton xərcləmə imkanınız məhduddur."
                    : $"{teyinat.Ad}{edSuffix} ({(teyinat.SaatDeyeri * eded):0.##} saat) qazandınız. Səbəb: {dto.Sebeb}";

                await _bildirisRouter.NotifyIsciAsync(
                    dto.IsciId, nov, bashliq, metn,
                    redirectUrl: "/User/Jeton/Index");

                return Result.Ok(eded > 1
                    ? $"{teyinat.Ad} × {eded} ({(teyinat.SaatDeyeri * eded):0.##} saat) uğurla verildi."
                    : $"{teyinat.Ad} uğurla verildi.");
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
                .Include(x => x.Isci)
                .Include(x => x.Icaze)
                .Include(x => x.RedimTelebi);

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

            return jetonlar.Sum(x => x.QalanSaat ?? x.JetonTeyinati.SaatDeyeri);
        }

        // İcazə ləğv olunanda jetonu geri qaytarır (reverse-FIFO: ən son qazanılandan).
        // QEYD: dəyişiklikləri stage edir, YaddaSaxlaAsync ÇAĞIRMIR — çağıran tək
        // tranzaksiyada saxlamalıdır (atomik icazə ləğvi + jeton bərpası üçün).
        public async Task<Result> IcazeJetonuGeriQaytarAsync(int isciId, decimal saat)
        {
            if (saat <= 0) return Result.Ok();
            var jetonlar = await _unitOfWork.Repository<IsciJetonu>()
                .Query()
                .Include(x => x.JetonTeyinati)
                .Where(x => x.IsciId == isciId
                    && x.JetonTeyinati.Nov == JetonNovu.Musbat
                    && x.RedimTelebiId == null
                    && (x.Status == IsciJetonuStatus.IstifadeOlunub || x.Status == IsciJetonuStatus.Aktiv))
                .OrderByDescending(x => x.QazanmaTarixi)
                .ToListAsync();

            decimal qalan = saat;
            foreach (var j in jetonlar)
            {
                if (qalan <= 0) break;
                decimal tam    = j.JetonTeyinati.SaatDeyeri;
                decimal movcut = j.QalanSaat ?? tam;
                decimal bosluq = tam - movcut;          // bu jetonda neçə saat geri qoyula bilər
                if (bosluq <= 0) continue;
                decimal qoy = Math.Min(bosluq, qalan);
                j.QalanSaat = movcut + qoy;
                if (j.Status == IsciJetonuStatus.IstifadeOlunub && j.QalanSaat > 0)
                    j.Status = IsciJetonuStatus.Aktiv;
                qalan -= qoy;
                await _unitOfWork.Repository<IsciJetonu>().YenileAsync(j);
            }
            return Result.Ok();
        }

        // ── Redim ─────────────────────────────────────────────────────────────

        public async Task<Result> RedimTelebiYaratAsync(int isciId, JetonRedimTelebiCreateDto dto)
        {
            try
            {
                if (dto == null)
                    return Result.Fail("Sorğu məlumatları alınmadı.");

                if (dto.JetonIds == null || !dto.JetonIds.Any())
                    return Result.Fail("Ən azı bir jeton seçilməlidir.");

                // "Maaşa əlavə" hələlik deaktivdir (gələcəkdə açılacaq) — UI-da da gizlidir
                if (dto.RedimNovu == RedimNovu.MaasaElave)
                    return Result.Fail("Maaşa əlavə hələlik deaktivdir — yalnız icazə seçimi mümkündür.");

                // İcazə sorğularında tarix və saat məcburidir
                if (dto.RedimNovu == RedimNovu.Icaze)
                {
                    if (dto.IcazeTarixi == null || dto.BaslamaSaati == null || dto.BitisSaati == null)
                        return Result.Fail("İcazə sorğusunda tarix və saat aralığı daxil edilməlidir.");

                    if (dto.IcazeTarixi.Value.Date < DateTime.Today)
                        return Result.Fail("Keçmiş tarix üçün icazə tələb edilə bilməz.");

                    if (dto.BitisSaati <= dto.BaslamaSaati)
                        return Result.Fail("Bitmə saatı başlama saatından sonra olmalıdır.");
                }

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

                var baseSaat = jetonlar.Sum(x => x.QalanSaat ?? x.JetonTeyinati.SaatDeyeri);

                // Reytinq əmsalını tətbiq et
                var (pulAmsali, saatAmsali) = await _reytingService.IsciAmsallariGetirAsync(isciId);
                var cemiSaat = dto.RedimNovu == RedimNovu.Icaze
                    ? baseSaat * saatAmsali
                    : baseSaat * pulAmsali;

                // İcazə sorğusunda istənilən İŞ saatı (nahar fasiləsi çıxılmaqla) jeton ödənişindən
                // çox ola bilməz. Naharın çıxılması günlük tokenin tam iş gününə (09:00–17:45 = 8 iş
                // saatı) uyğun gəlməsini təmin edir.
                if (dto.RedimNovu == RedimNovu.Icaze)
                {
                    var isParam = await _unitOfWork.Repository<IsParametri>()
                        .Query().FirstOrDefaultAsync() ?? new IsParametri();
                    var istenilenSaat = IsSaatiHesabla(dto.BaslamaSaati!.Value, dto.BitisSaati!.Value, isParam);
                    if (istenilenSaat > cemiSaat)
                        return Result.Fail(
                            $"Seçdiyiniz iş saatı ({istenilenSaat:0.##} saat) " +
                            $"jeton ödənişindən ({cemiSaat:0.##} saat) çoxdur.");
                }

                var redim = new JetonRedimTelebi
                {
                    IsciId = isciId,
                    RedimNovu = dto.RedimNovu,
                    CemiSaat = cemiSaat,
                    Status = RedimStatus.Gozlenilir,
                    TelabTarixi = DateTime.Now,
                    IcazeTarixi = dto.IcazeTarixi,
                    BaslamaSaati = dto.BaslamaSaati,
                    BitisSaati = dto.BitisSaati
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

                // İlk növbədə Rəhbərə bildiriş gedir (HR yox — rəhbərdən sonra)
                var redimNovuAd = dto.RedimNovu == RedimNovu.Icaze ? "icazə" : "maaş bonusu";
                await _bildirisRouter.NotifyRolesAsync(
                    new[] { RoleNames.Rehber },
                    BildirisNovu.JetonVerildi,
                    "Yeni Jeton Redim Sorğusu",
                    $"İşçi {cemiSaat:0.##} saatlıq {jetonlar.Count} jetonu {redimNovuAd} kimi xərcləmək istəyir.",
                    redirectUrl: "/HR/Jeton/Index?tab=redimler",
                    exceptIsciId: isciId);

                var novLabel = dto.RedimNovu == RedimNovu.Icaze ? "İcazə" : "Maaşa əlavə";
                var amsalGoster = dto.RedimNovu == RedimNovu.Icaze ? saatAmsali : pulAmsali;
                var amsalMetn = amsalGoster != 1.00m ? $" (reytinq əmsalı: {amsalGoster}x)" : "";
                return Result.Ok($"Sorğu göndərildi. Cəmi: {cemiSaat:0.##} saat{amsalMetn} ({novLabel}).");
            }
            catch (Exception ex)
            {
                return Result.Fail($"Xəta: {ex.Message}");
            }
        }

        // İki saat arasındakı İŞ saatlarını hesablayır — nahar fasiləsi aralığa düşürsə çıxılır.
        private static decimal IsSaatiHesabla(TimeSpan bas, TimeSpan bitis, IsParametri p)
        {
            var saat = (decimal)(bitis - bas).TotalHours;
            var naharBas = p.NaharBaslamaSaati;
            var naharBitis = naharBas + TimeSpan.FromMinutes(p.NaharMuddetDeqiqe);
            var oBas = bas > naharBas ? bas : naharBas;
            var oBitis = bitis < naharBitis ? bitis : naharBitis;
            if (oBitis > oBas) saat -= (decimal)(oBitis - oBas).TotalHours;
            return saat < 0 ? 0 : saat;
        }

        // Günlük token üçün modalda avtomatik tam iş günü doldurmaq (UI-a verilir).
        public async Task<(string Giris, string Cixis)> StandartIsSaatiGetirAsync()
        {
            var p = await _unitOfWork.Repository<IsParametri>().Query().FirstOrDefaultAsync() ?? new IsParametri();
            return (p.StandartGirisVaxti.ToString(@"hh\:mm"), p.StandartCixisVaxti.ToString(@"hh\:mm"));
        }

        public async Task<Result> RedimRehberTesdiqleAsync(int redimId, int rehberUserId)
        {
            try
            {
                var redim = await _unitOfWork.Repository<JetonRedimTelebi>()
                    .Query()
                    .Include(x => x.Isci)
                    .FirstOrDefaultAsync(x => x.Id == redimId);

                if (redim == null) return Result.Fail("Redim sorğusu tapılmadı.");
                if (redim.Status != RedimStatus.Gozlenilir)
                    return Result.Fail("Bu sorğu artıq emal edilib.");
                if (redim.RehberTesdiq.HasValue)
                    return Result.Fail("Sorğu artıq rəhbər tərəfindən baxılıb.");

                redim.RehberTesdiq = true;
                redim.RehberUserId = rehberUserId;
                redim.RehberTesdiqTarixi = DateTime.Now;

                await _unitOfWork.Repository<JetonRedimTelebi>().YenileAsync(redim);
                await _unitOfWork.YaddaSaxlaAsync();

                // HR-ə bildiriş — rəhbər təsdiqlədi, sıra HR-də
                var redimNovuAd = redim.RedimNovu == RedimNovu.Icaze ? "icazə" : "maaş bonusu";
                await _bildirisRouter.NotifyRolesAsync(
                    new[] { RoleNames.HR, RoleNames.Admin },
                    BildirisNovu.JetonVerildi,
                    "Rəhbər təsdiqindən keçdi",
                    $"{redim.Isci.Ad} {redim.Isci.Soyad}-in {redim.CemiSaat:0.##} saatlıq {redimNovuAd} sorğusu rəhbər təsdiqindən keçib.",
                    redirectUrl: "/HR/Jeton/Index?tab=redimler",
                    exceptIsciId: redim.IsciId);

                return Result.Ok("Sorğu təsdiqləndi və HR-ə göndərildi.");
            }
            catch (Exception ex)
            {
                return Result.Fail($"Xəta: {ex.Message}");
            }
        }

        public async Task<Result> RedimRehberReddEtAsync(int redimId, string qeyd, int rehberUserId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(qeyd))
                    return Result.Fail("Rədd etmə səbəbi daxil edilməlidir.");

                var redim = await _unitOfWork.Repository<JetonRedimTelebi>()
                    .Query()
                    .Include(x => x.XerclenenJetonlar)
                    .Include(x => x.Isci)
                    .FirstOrDefaultAsync(x => x.Id == redimId);

                if (redim == null) return Result.Fail("Redim sorğusu tapılmadı.");
                if (redim.Status != RedimStatus.Gozlenilir)
                    return Result.Fail("Bu sorğu artıq emal edilib.");

                redim.Status = RedimStatus.Redd;
                redim.RehberTesdiq = false;
                redim.RehberUserId = rehberUserId;
                redim.RehberTesdiqTarixi = DateTime.Now;
                redim.RehberQeyd = qeyd;
                redim.NeticeTarixi = DateTime.Now;

                // Jetonlar geri açılır
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
                    "❌ Jeton sorğunuz rəhbər tərəfindən rədd edildi",
                    $"Səbəb: {qeyd}",
                    redirectUrl: "/User/Jeton/Index");

                return Result.Ok("Sorğu rədd edildi.");
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

                if (redim.RehberTesdiq != true)
                    return Result.Fail("Bu sorğu hələ rəhbər tərəfindən təsdiqlənməyib.");

                redim.Status = RedimStatus.Tesdiqlendi;
                redim.NeticeTarixi = DateTime.Now;
                redim.TesdiqleyenUserId = tesdiqleyenUserId;

                // Jetonları "İstifadə olunub" et
                foreach (var j in redim.XerclenenJetonlar)
                {
                    j.Status = IsciJetonuStatus.IstifadeOlunub;
                    j.XerclenmeTarixi = DateTime.Now;
                    await _unitOfWork.Repository<IsciJetonu>().YenileAsync(j);
                }

                // İcazə sorğusu → avtomatik Icaze qeydi yarat (tam təsdiqlənmiş halda)
                if (redim.RedimNovu == RedimNovu.Icaze
                    && redim.IcazeTarixi.HasValue
                    && redim.BaslamaSaati.HasValue
                    && redim.BitisSaati.HasValue)
                {
                    var icaze = new Icaze
                    {
                        IsciId = redim.IsciId,
                        IcazeTarixi = redim.IcazeTarixi.Value,
                        BaslamaSaati = redim.BaslamaSaati.Value,
                        BitisSaati = redim.BitisSaati.Value,
                        Sebeb = $"Jetonla ödənilib (sorğu #{redim.Id})",
                        Status = IcazeStatus.Tesdiqlenib,
                        SobeReisiTesdiq = true,
                        SobeReisiTesdiqTarixi = DateTime.Now,
                        RehberTesdiq = true,
                        RehberTesdiqTarixi = redim.RehberTesdiqTarixi,
                        HrTesdiq = true,
                        HrTesdiqTarixi = DateTime.Now,
                        JetonOdenenSaat = redim.CemiSaat
                    };

                    // SobeReisiId, RehberId, HrId üçün Isci ID-ləri lazımdır.
                    // Bunlar AppUser.IsciId üzərindən tapılmalıdır.
                    var rehberIsciId = redim.RehberUserId.HasValue
                        ? await _userManager.Users
                            .Where(u => u.Id == redim.RehberUserId.Value)
                            .Select(u => u.IsciId)
                            .FirstOrDefaultAsync()
                        : null;
                    var hrIsciId = await _userManager.Users
                        .Where(u => u.Id == tesdiqleyenUserId)
                        .Select(u => u.IsciId)
                        .FirstOrDefaultAsync();

                    icaze.RehberId = rehberIsciId;
                    icaze.SobeReisiId = rehberIsciId; // ŞR mərhələsi keçilməyib, eyni şəxsi qeyd edirik
                    icaze.HrId = hrIsciId;

                    await _unitOfWork.Repository<Icaze>().YaratAsync(icaze);

                    // Möhkəmlik: həmin günü davamiyyətdə dərhal "İcazəli" et (Qayib qalıb maaşdan
                    // kəsilməsin). Background marker yalnız qeydi olmayan günləri yazır — burada upsert.
                    // Yalnız qeyd yoxdursa və ya Qayib-dirsə dəyişir; işçi gəlib (Isde/Gecikme) qeydə toxunulmur.
                    var icazeGunu = redim.IcazeTarixi.Value.Date;
                    var dav = await _unitOfWork.Repository<Davamiyyet>()
                        .Query().FirstOrDefaultAsync(x => x.IsciId == redim.IsciId
                            && x.Tarix.Date == icazeGunu && !x.Silinib);
                    if (dav == null)
                    {
                        await _unitOfWork.Repository<Davamiyyet>().YaratAsync(new Davamiyyet
                        {
                            IsciId          = redim.IsciId,
                            Tarix           = icazeGunu,
                            Status          = DavamiyyetStatus.Icazeli,
                            MaasdanKes      = false,
                            YaradilmaTarixi = DateTime.Now
                        });
                    }
                    else if (dav.Status == DavamiyyetStatus.Qayib)
                    {
                        dav.Status     = DavamiyyetStatus.Icazeli;
                        dav.MaasdanKes = false;
                        await _unitOfWork.Repository<Davamiyyet>().YenileAsync(dav);
                    }
                }

                await _unitOfWork.Repository<JetonRedimTelebi>().YenileAsync(redim);
                await _unitOfWork.YaddaSaxlaAsync();

                var novAd = redim.RedimNovu == RedimNovu.Icaze ? "icazə" : "maaş bonusu";
                await _bildirisRouter.NotifyIsciAsync(
                    redim.IsciId,
                    BildirisNovu.JetonRedimTesdiqlendi,
                    "✅ Jeton sorğunuz təsdiqləndi",
                    $"{redim.CemiSaat:0.##} saatlıq jeton sorğunuz {novAd} kimi təsdiqləndi.",
                    redirectUrl: "/User/Jeton/Index");

                return Result.Ok("Redim sorğusu təsdiqləndi.");
            }
            catch (Exception ex)
            {
                return Result.Fail($"Xəta: {ex.Message}");
            }
        }

        public async Task<Result<decimal>> IcazeUcunFifoJetonXercleAsync(int isciId, decimal teleblesaat, int? icazeId = null)
        {
            try
            {
                if (teleblesaat <= 0)
                    return Result<decimal>.Ok(0);

                // Aktiv jetonları FIFO sırada — ən köhnədən başla
                var jetonlar = await _unitOfWork.Repository<IsciJetonu>()
                    .Query()
                    .Include(x => x.JetonTeyinati)
                    .Where(x => x.IsciId == isciId
                        && x.Status == IsciJetonuStatus.Aktiv
                        && x.JetonTeyinati.Nov == JetonNovu.Musbat
                        && x.RedimTelebiId == null) // başqa sorğuya bağlı jetonları toxunma
                    .OrderBy(x => x.QazanmaTarixi)
                    .ToListAsync();

                // Mövcud balans yoxlaması
                decimal totalMovcut = jetonlar.Sum(x => x.QalanSaat ?? x.JetonTeyinati.SaatDeyeri);
                if (totalMovcut < teleblesaat)
                    return Result<decimal>.Fail(
                        $"İşçinin aktiv balansı kifayət etmir ({totalMovcut:0.##} < {teleblesaat:0.##} saat).");

                // FIFO — qismən xərcləmə dəstəklənir
                decimal qalan = teleblesaat;
                decimal cemXerclenen = 0;

                foreach (var j in jetonlar)
                {
                    if (qalan <= 0) break;

                    decimal movcut = j.QalanSaat ?? j.JetonTeyinati.SaatDeyeri;

                    if (movcut <= qalan)
                    {
                        // Bu jetonu tam xərclə
                        j.Status = IsciJetonuStatus.IstifadeOlunub;
                        j.QalanSaat = 0;
                        cemXerclenen += movcut;
                        qalan -= movcut;
                    }
                    else
                    {
                        // Bu jetonu qismən xərclə — qalan hissə aktivdir
                        j.QalanSaat = movcut - qalan;
                        cemXerclenen += qalan;
                        qalan = 0;
                    }

                    if (icazeId.HasValue)
                        j.IcazeId = icazeId.Value;
                    j.XerclenmeTarixi = DateTime.Now;

                    await _unitOfWork.Repository<IsciJetonu>().YenileAsync(j);
                }

                await _unitOfWork.YaddaSaxlaAsync();

                return Result<decimal>.Ok(cemXerclenen);
            }
            catch (Exception ex)
            {
                return Result<decimal>.Fail($"Xəta: {ex.Message}");
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

        public async Task<IList<JetonRedimTelebiListDto>> GozleyenRedimlerGetirAsync(int? rehberDepartamentId, bool asRehber, bool asHrAdmin)
        {
            var query = _unitOfWork.Repository<JetonRedimTelebi>()
                .Query()
                .Include(x => x.Isci)
                    .ThenInclude(i => i.IsciTeyinatlari.Where(t => t.Aktivdir))
                .Include(x => x.XerclenenJetonlar)
                    .ThenInclude(j => j.JetonTeyinati)
                .Where(x => x.Status == RedimStatus.Gozlenilir);

            // İstifadəçi həm rəhbər (RehberTesdiq==null; öz departamenti, admin hamısı), həm də
            // HR/Admin (RehberTesdiq==true) ola bilər — hər iki mərhələ göstərilir (rol toqquşması həll).
            if (asRehber && asHrAdmin)
            {
                if (rehberDepartamentId.HasValue)
                    query = query.Where(x =>
                        (x.RehberTesdiq == null && x.Isci.IsciTeyinatlari
                            .Any(t => t.Aktivdir && t.DepartamentId == rehberDepartamentId.Value))
                        || x.RehberTesdiq == true);
                else
                    query = query.Where(x => x.RehberTesdiq == null || x.RehberTesdiq == true);
            }
            else if (asRehber)
            {
                // Yalnız rəhbər: öz departamentinin hələ baxılmamış sorğuları
                query = query.Where(x => x.RehberTesdiq == null);
                if (rehberDepartamentId.HasValue)
                    query = query.Where(x => x.Isci.IsciTeyinatlari
                        .Any(t => t.Aktivdir && t.DepartamentId == rehberDepartamentId.Value));
            }
            else
            {
                // Yalnız HR/Admin: rəhbər təsdiqindən keçmiş sorğular
                query = query.Where(x => x.RehberTesdiq == true);
            }

            var list = await query
                .OrderBy(x => x.TelabTarixi)
                .ToListAsync();

            // Rəhbər adları (təsdiq etmiş) üçün lookup
            var rehberUserIds = list
                .Where(x => x.RehberUserId.HasValue)
                .Select(x => x.RehberUserId!.Value)
                .Distinct()
                .ToList();

            var rehberAdMap = new Dictionary<int, string>();
            if (rehberUserIds.Count > 0)
            {
                var rehberUsers = await _userManager.Users
                    .Where(u => rehberUserIds.Contains(u.Id))
                    .ToListAsync();
                rehberAdMap = rehberUsers.ToDictionary(u => u.Id, u => u.UserName ?? "—");
            }

            return list.Select(x =>
            {
                var dto = MapRedim(x);
                if (x.RehberUserId.HasValue)
                    dto.RehberAd = rehberAdMap.GetValueOrDefault(x.RehberUserId.Value, "—");
                return dto;
            }).ToList();
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

        public async Task<Result> JetonTeyinatiYaratAsync(JetonTeyinatiCreateDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Ad))
                    return Result.Fail("Ad boş ola bilməz.");

                if (dto.Nov == JetonNovu.Musbat && dto.SaatDeyeri < 0)
                    return Result.Fail("Müsbət jeton üçün saat dəyəri mənfi ola bilməz.");

                var teyinat = new JetonTeyinati
                {
                    Ad                = dto.Ad.Trim(),
                    Nov               = dto.Nov,
                    Rengi             = dto.Rengi,
                    SaatDeyeri        = dto.SaatDeyeri,
                    Vahid             = dto.Vahid,
                    Ikon              = string.IsNullOrWhiteSpace(dto.Ikon) ? "bi bi-award-fill" : dto.Ikon.Trim(),
                    RengKodu          = string.IsNullOrWhiteSpace(dto.RengKodu) ? "#6b7280" : dto.RengKodu.Trim(),
                    Tesvir            = dto.Tesvir?.Trim(),
                    BirbasaOdenishli  = dto.BirbasaOdenishli,
                    Aktivdir          = true
                };

                await _unitOfWork.Repository<JetonTeyinati>().YaratAsync(teyinat);
                await _unitOfWork.YaddaSaxlaAsync();

                return Result.Ok($"{teyinat.Ad} jeton növü yaradıldı.");
            }
            catch (Exception ex)
            {
                return Result.Fail($"Xəta: {ex.Message}");
            }
        }

        public async Task<Result> JetonTeyinatiYenileAsync(JetonTeyinatiUpdateDto dto)
        {
            try
            {
                var teyinat = await _unitOfWork.Repository<JetonTeyinati>()
                    .Query().FirstOrDefaultAsync(x => x.Id == dto.Id);
                if (teyinat == null)
                    return Result.Fail("Jeton növü tapılmadı.");

                if (string.IsNullOrWhiteSpace(dto.Ad))
                    return Result.Fail("Ad boş ola bilməz.");

                // Müsbət jetonlar üçün SaatDeyeri 0-dan kiçik ola bilməz;
                // mənfi (cəza) jetonlar 0 və ya mənfi olur
                if (teyinat.Nov == JetonNovu.Musbat && dto.SaatDeyeri < 0)
                    return Result.Fail("Müsbət jeton üçün saat dəyəri mənfi ola bilməz.");

                teyinat.Ad               = dto.Ad.Trim();
                teyinat.SaatDeyeri       = dto.SaatDeyeri;
                teyinat.Vahid            = dto.Vahid;
                teyinat.Tesvir           = dto.Tesvir;
                teyinat.Ikon             = string.IsNullOrWhiteSpace(dto.Ikon) ? teyinat.Ikon : dto.Ikon.Trim();
                teyinat.RengKodu         = string.IsNullOrWhiteSpace(dto.RengKodu) ? teyinat.RengKodu : dto.RengKodu.Trim();
                teyinat.BirbasaOdenishli = dto.BirbasaOdenishli;
                teyinat.Aktivdir         = dto.Aktivdir;

                await _unitOfWork.Repository<JetonTeyinati>().YenileAsync(teyinat);
                await _unitOfWork.YaddaSaxlaAsync();

                return Result.Ok($"{teyinat.Ad} yeniləndi.");
            }
            catch (Exception ex)
            {
                return Result.Fail($"Xəta: {ex.Message}");
            }
        }

        public async Task<IList<JetonRedimTelebiListDto>> ButunRedimlerTarixcesiAsync()
        {
            var list = await _unitOfWork.Repository<JetonRedimTelebi>()
                .Query()
                .Include(x => x.Isci)
                .Include(x => x.XerclenenJetonlar)
                    .ThenInclude(j => j.JetonTeyinati)
                .OrderByDescending(x => x.TelabTarixi)
                .ToListAsync();

            // Təsdiqləyən/rədd edənlərin adlarını topla
            var verenIds = list
                .Where(x => x.TesdiqleyenUserId.HasValue)
                .Select(x => x.TesdiqleyenUserId!.Value)
                .Distinct()
                .ToList();

            var userMap = new Dictionary<int, string>();
            if (verenIds.Count > 0)
            {
                var users = await _userManager.Users
                    .Where(u => verenIds.Contains(u.Id))
                    .ToListAsync();
                userMap = users.ToDictionary(u => u.Id, u => u.UserName ?? "—");
            }

            return list.Select(x =>
            {
                var dto = MapRedim(x);
                if (x.TesdiqleyenUserId.HasValue)
                    dto.TesdiqleyenAd = userMap.GetValueOrDefault(x.TesdiqleyenUserId.Value, "—");
                return dto;
            }).ToList();
        }

        // ── Köməkçi mapper metodlar ───────────────────────────────────────────

        private static JetonTeyinatiListDto MapTeyinat(JetonTeyinati x) => new()
        {
            Id               = x.Id,
            Ad               = x.Ad,
            Nov              = x.Nov,
            Rengi            = x.Rengi,
            SaatDeyeri       = x.SaatDeyeri,
            Vahid            = x.Vahid,
            Ikon             = x.Ikon,
            RengKodu         = x.RengKodu,
            Tesvir           = x.Tesvir,
            BirbasaOdenishli = x.BirbasaOdenishli,
            Aktivdir         = x.Aktivdir
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
            JetonVahid      = x.JetonTeyinati.Vahid,
            QalanSaat       = x.QalanSaat,
            QazanmaTarixi   = x.QazanmaTarixi,
            Sebeb = x.Sebeb,
            Status = x.Status,
            RedimTelebiId   = x.RedimTelebiId,
            RedimNetice     = x.RedimTelebi?.NeticeTarixi,
            IcazeId         = x.IcazeId,
            IcazeTarixi       = x.Icaze?.IcazeTarixi,
            IcazeBaslamaSaati = x.Icaze != null ? x.Icaze.BaslamaSaati.ToString(@"hh\:mm") : null,
            IcazeBitisSaati   = x.Icaze != null ? x.Icaze.BitisSaati.ToString(@"hh\:mm") : null,
            XerclenmeTarixi   = x.XerclenmeTarixi,
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
            IcazeTarixi = x.IcazeTarixi,
            BaslamaSaati = x.BaslamaSaati,
            BitisSaati = x.BitisSaati,
            RehberTesdiq = x.RehberTesdiq,
            RehberTesdiqTarixi = x.RehberTesdiqTarixi,
            RehberQeyd = x.RehberQeyd,
            XerclenenJetonlar = x.XerclenenJetonlar.Select(MapJeton).ToList()
        };
    }
}
