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
            // Sıralama: əvvəl müsbət (mükafat) jetonlar, saat dəyərinə görə BÖYÜKDƏN
            // KİÇİYƏ (Platin 8 → Qızıl 4 → Gümüş 1 → Bürünc 0.5), sonda mənfi (Qara cəza).
            var list = await _unitOfWork.Repository<JetonTeyinati>()
                .Query()
                // Sistemli (məs. "36 Saat Hüququ {il}") HR-in adi kataloqunda
                // görünmür — avtomatik yaradılır, əl ilə verilmir/redaktə olunmur.
                .Where(x => x.Aktivdir && !x.Sistemli)
                .OrderBy(x => x.Nov)
                .ThenByDescending(x => x.SaatDeyeri)
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

                if (teyinat.Sistemli)
                    return Result.Fail("Bu jeton növü sistem tərəfindən idarə olunur, əl ilə verilə bilməz.");

                var eded = dto.Eded < 1 ? 1 : (dto.Eded > 50 ? 50 : dto.Eded);

                // Qara jeton üçün eded həmişə 1 — intizam xəbərdarlığı dublicate edilməsin
                if (teyinat.Nov == JetonNovu.Menfi) eded = 1;

                var yaradilanlar = new List<IsciJetonu>();
                for (int i = 0; i < eded; i++)
                {
                    var jeton = new IsciJetonu
                    {
                        IsciId = dto.IsciId,
                        JetonTeyinatiId = dto.JetonTeyinatiId,
                        Sebeb = dto.Sebeb,
                        VerenUserId = verenUserId,
                        QazanmaTarixi = DateTime.Now,
                        Status = IsciJetonuStatus.Aktiv,
                        // Menfi növdə kəsinti miqdarı VERİLMƏ ANINDA dondurulur —
                        // kataloq sonradan dəyişsə də bu Qara Jetonun tarixi
                        // "kəsilən saat"i dəyişməməlidir (07.10.2026, KRİTİK).
                        MenfiMiqdar = teyinat.Nov == JetonNovu.Menfi ? Math.Abs(teyinat.SaatDeyeri) : (decimal?)null
                    };
                    await _unitOfWork.Repository<IsciJetonu>().YaratAsync(jeton);
                    yaradilanlar.Add(jeton);
                }
                await _unitOfWork.YaddaSaxlaAsync();

                // Bildiriş — bir bildiriş çoxsaylı jetonu əhatə edir
                var isQara = teyinat.Nov == JetonNovu.Menfi;

                // ── Qara Jeton dəyərli kəsinti (07.10.2026, istifadəçi qərarı) ──────
                // Qara Jeton verilən AN dəyəri kəsilir: (1) müsbət balansdan FIFO,
                // (2) qalıbsa illik "36 Saat Hüququ"ndan, (3) yenə qalıbsa gözləyən
                // borc yazılır (il sonuna qədər, növbəti müsbət jetondan ödənilir).
                if (isQara)
                {
                    await QaraJetonKesintisiTetbiqEtAsync(dto.IsciId, yaradilanlar[0].Id, yaradilanlar[0].MenfiMiqdar ?? 0);
                }
                else
                {
                    // Növbəti müsbət jeton — əvvəlcə gözləyən Qara Jeton borcunu ödə.
                    // Hər yaradılan sətir (eded>1 ola bilər) öz növbəsində borcu azaldır;
                    // tam yeyərsə sıfır qalıqlı (görünən) qalır, qismən yesə qalan hissə
                    // normal istifadə oluna bilən qalır (istifadəçi qərarı).
                    foreach (var yeni in yaradilanlar)
                        await QaraJetonBorcunuOdeAsync(yeni, teyinat);
                }
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

                if (jeton.JetonTeyinati.Sistemli)
                    return Result.Fail("Bu, işçinin qanuni illik hüququdur — ləğv edilə bilməz.");

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
                // "36 Saat Hüququ" işçinin "Cüzdanım" kart siyahısında (User/Jeton/GetJetonlarim)
                // mükafat kimi görünməsin — bu, ayrıca (Dashboard-dakı) illik hüquqdur, jeton
                // yalnız daxili uçot üçündür. AktivSaatBalansiAsync-dəki eyni istisnaya uyğun.
                .Where(x => x.IsciId == isciId && x.Status == IsciJetonuStatus.Aktiv && !x.JetonTeyinati.Sistemli)
                .OrderByDescending(x => x.QazanmaTarixi)
                .ToListAsync();

            var dtos = list.Select(MapJeton).ToList();
            await QaraJetonKesilenSaatDoldurAsync(dtos);
            return dtos;
        }

        // Menfi (Qara) jetonlar üçün "həqiqətən kəsilən saat"i doldurur —
        // istifadəçi tələbi (07.10.2026): "verildi və çıxıldı" tarixçə kimi
        // görünsün. Düstur: JetonSaatDeyeri − (bu Qara Jetona bağlı QaraJetonBorcu
        // sətirlərinin QalanSaat cəmi). Odenildi sətirlərdə QalanSaat=0 olduğu
        // üçün cəmə təsir etmir; MuddetiBitib (bağışlanmış) sətirlərdə QalanSaat
        // hələ qalır — o hissə "kəsilən" sayılmır (həqiqətən yığılmayıb).
        private async Task QaraJetonKesilenSaatDoldurAsync(IList<IsciJetonuListDto> dtos)
        {
            var qaraIds = dtos.Where(d => d.JetonNovu == JetonNovu.Menfi).Select(d => d.Id).ToList();
            if (qaraIds.Count == 0) return;

            var borclar = await _unitOfWork.Repository<QaraJetonBorcu>()
                .Query()
                .Where(x => qaraIds.Contains(x.QaraJetonId))
                .GroupBy(x => x.QaraJetonId)
                .Select(g => new { QaraJetonId = g.Key, Cemi = g.Sum(x => x.QalanSaat) })
                .ToListAsync();
            var borcDict = borclar.ToDictionary(x => x.QaraJetonId, x => x.Cemi);

            foreach (var dto in dtos.Where(d => d.JetonNovu == JetonNovu.Menfi))
            {
                var gozleyen = borcDict.TryGetValue(dto.Id, out var c) ? c : 0m;
                // JetonSaatDeyeri Menfi növdə mənfi yazıla bilər (bax yuxarı
                // QaraJetonKesintisiTetbiqEtAsync-dəki qeyd) — miqdar üçün Abs.
                dto.QaraJetonKesilenSaat = Math.Max(0, Math.Abs(dto.JetonSaatDeyeri) - gozleyen);
            }
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

            var dtos = list.Select(MapJeton).ToList();
            await QaraJetonKesilenSaatDoldurAsync(dtos);
            return dtos;
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
                    && x.JetonTeyinati.Nov == JetonNovu.Musbat
                    && !x.JetonTeyinati.Sistemli)
                .ToListAsync();

            return jetonlar.Sum(x => x.QalanSaat ?? x.JetonTeyinati.SaatDeyeri);
        }

        // ── Qara Jeton — dəyərli kəsinti (07.10.2026) ───────────────────────────

        private const decimal IllikHuquqSaat = 36m;

        // Göstərilən ilin "36 Saat Hüququ {il}" JetonTeyinati-sini tapır, yoxdursa
        // yaradır. Bütün işçilər üçün ORTAQ şablondur (IsciJetonu sətri hər işçiyə
        // ayrıdır — bax EnsureIllikHuquqJetonuAsync).
        private async Task<JetonTeyinati> EnsureIllikHuquqTeyinatiAsync(int il)
        {
            var ad = $"36 Saat Hüququ {il}";
            var teyinat = await _unitOfWork.Repository<JetonTeyinati>()
                .Query().FirstOrDefaultAsync(x => x.Ad == ad && x.Sistemli);
            if (teyinat != null) return teyinat;

            teyinat = new JetonTeyinati
            {
                Ad = ad,
                Nov = JetonNovu.Musbat,
                Rengi = JetonRengi.Gumus,
                SaatDeyeri = IllikHuquqSaat,
                Vahid = JetonVahid.Saat,
                Ikon = "bi bi-calendar-check-fill",
                RengKodu = "#64748b",
                Tesvir = $"{il}-ci il üçün qanuni illik icazə hüququ (avtomatik yaradılıb) — " +
                         "Qara Jeton bu hüququn qalığından kəsilə bilər.",
                BirbasaOdenishli = false,
                Sistemli = true,
                Aktivdir = true
            };
            await _unitOfWork.Repository<JetonTeyinati>().YaratAsync(teyinat);
            await _unitOfWork.YaddaSaxlaAsync();
            return teyinat;
        }

        // İşçinin göstərilən ilin "36 Saat Hüququ" sətrini (IsciJetonu) tapır,
        // yoxdursa tam dəyərlə (QalanSaat=null → SaatDeyeri) yaradır.
        private async Task<IsciJetonu> EnsureIllikHuquqJetonuAsync(int isciId, int il)
        {
            var teyinat = await EnsureIllikHuquqTeyinatiAsync(il);

            var jeton = await _unitOfWork.Repository<IsciJetonu>()
                .Query()
                .Include(x => x.JetonTeyinati)
                .FirstOrDefaultAsync(x => x.IsciId == isciId && x.JetonTeyinatiId == teyinat.Id);
            if (jeton != null) return jeton;

            jeton = new IsciJetonu
            {
                IsciId = isciId,
                JetonTeyinatiId = teyinat.Id,
                Sebeb = $"{il}-ci il üçün qanuni illik icazə hüququ (avtomatik)",
                VerenUserId = 0,
                QazanmaTarixi = new DateTime(il, 1, 1),
                Status = IsciJetonuStatus.Aktiv
            };
            await _unitOfWork.Repository<IsciJetonu>().YaratAsync(jeton);
            await _unitOfWork.YaddaSaxlaAsync();
            jeton.JetonTeyinati = teyinat;
            return jeton;
        }

        public async Task<decimal> IllikHuquqQalanSaatAsync(int isciId, int? il = null)
        {
            var jeton = await EnsureIllikHuquqJetonuAsync(isciId, il ?? DateTime.Today.Year);
            return jeton.QalanSaat ?? jeton.JetonTeyinati.SaatDeyeri;
        }

        public async Task<decimal> GozleyenQaraJetonBorcuSaatAsync(int isciId)
        {
            var cariIl = DateTime.Today.Year;
            return await _unitOfWork.Repository<QaraJetonBorcu>()
                .Query()
                .Where(x => x.IsciId == isciId
                    && x.Status == QaraJetonBorcuStatus.Gozleyir
                    && x.Il == cariIl)
                .SumAsync(x => x.QalanSaat);
        }

        // Qara Jeton verilən AN dəyərini kəsir: (1) müsbət (Sistemli olmayan)
        // jetonlardan FIFO, (2) qalıbsa cari ilin "36 Saat Hüququ" jetonundan,
        // (3) yenə qalıbsa QaraJetonBorcu "Gözləyir" yazılır.
        //
        // `deyer` (JetonTeyinati.SaatDeyeri) Menfi növdə HR tərəfindən TƏBİİ
        // olaraq MƏNFİ ədədlə yazılır (kataloq ekranında "-30 saat" göstərilir —
        // 07.10.2026, real hadisə: HR Qara Jetonun dəyərini "-30" yazdı, işarəni
        // "Menfi"ni əks etdirən DÜZGÜN konvensiya kimi gördü). Kəsinti MİQDARI isə
        // HƏMİŞƏ müsbətdir — ona görə `Math.Abs` ilə oxunur. Fix-dən əvvəl
        // `if (deyer <= 0) return;` mənfi dəyəri "təyin edilməyib" ilə qarışdırıb
        // BÜTÜN kəsintini səssizcə ləğv edirdi (07.10.2026-da verilmiş "test"
        // Qara Jetonu heç nəyi kəsmədi, balans dəyişmədi).
        private async Task QaraJetonKesintisiTetbiqEtAsync(int isciId, int qaraJetonuId, decimal deyer)
        {
            var miqdar = Math.Abs(deyer);
            if (miqdar <= 0) return;
            decimal qalan = miqdar;

            var musbetJetonlar = await _unitOfWork.Repository<IsciJetonu>()
                .Query()
                .Include(x => x.JetonTeyinati)
                .Where(x => x.IsciId == isciId
                    && x.Status == IsciJetonuStatus.Aktiv
                    && x.JetonTeyinati.Nov == JetonNovu.Musbat
                    && !x.JetonTeyinati.Sistemli
                    && x.RedimTelebiId == null)
                .OrderBy(x => x.QazanmaTarixi)
                .ToListAsync();

            foreach (var j in musbetJetonlar)
            {
                if (qalan <= 0) break;
                var movcut = j.QalanSaat ?? j.JetonTeyinati.SaatDeyeri;
                if (movcut <= 0) continue;

                var kesilen = Math.Min(movcut, qalan);
                var yeniQalan = movcut - kesilen;
                j.QalanSaat = yeniQalan;
                if (yeniQalan <= 0) j.Status = IsciJetonuStatus.IstifadeOlunub;
                j.QaraJetonId = qaraJetonuId;
                j.XerclenmeTarixi = DateTime.Now;
                await _unitOfWork.Repository<IsciJetonu>().YenileAsync(j);
                qalan -= kesilen;
            }

            if (qalan > 0)
            {
                var illikJeton = await EnsureIllikHuquqJetonuAsync(isciId, DateTime.Today.Year);
                var illikMovcut = illikJeton.QalanSaat ?? illikJeton.JetonTeyinati.SaatDeyeri;
                if (illikMovcut > 0)
                {
                    var kesilen = Math.Min(illikMovcut, qalan);
                    illikJeton.QalanSaat = illikMovcut - kesilen;
                    illikJeton.QaraJetonId = qaraJetonuId;
                    illikJeton.XerclenmeTarixi = DateTime.Now;
                    await _unitOfWork.Repository<IsciJetonu>().YenileAsync(illikJeton);
                    qalan -= kesilen;
                }
            }

            if (qalan > 0)
            {
                await _unitOfWork.Repository<QaraJetonBorcu>().YaratAsync(new QaraJetonBorcu
                {
                    IsciId = isciId,
                    Il = DateTime.Today.Year,
                    QalanSaat = qalan,
                    QaraJetonId = qaraJetonuId,
                    Status = QaraJetonBorcuStatus.Gozleyir
                });
            }

            await _unitOfWork.YaddaSaxlaAsync();
        }

        // Yeni verilən müsbət jetondan əvvəlcə işçinin gözləyən Qara Jeton
        // borcunu ödəyir. Keçən illərin ödənməmiş borcu bu çağırışda bağışlanır
        // (Status=MuddetiBitib) — "qara jeton cari ilin sonuna qədər qüvvədədir".
        // Yalnız BİR (ən köhnə) gözləyən borcu ödəyir — eyni anda bir neçə borc
        // varsa, bir yaradılan jeton bunlardan birini ödəyir (eded>1 olanda
        // hər sətir öz növbəsində işləyir).
        private async Task QaraJetonBorcunuOdeAsync(IsciJetonu yeniJeton, JetonTeyinati yeniTeyinat)
        {
            var cariIl = DateTime.Today.Year;

            var kohneBorclar = await _unitOfWork.Repository<QaraJetonBorcu>()
                .Query()
                .Where(x => x.IsciId == yeniJeton.IsciId
                    && x.Status == QaraJetonBorcuStatus.Gozleyir
                    && x.Il < cariIl)
                .ToListAsync();
            foreach (var kb in kohneBorclar)
            {
                kb.Status = QaraJetonBorcuStatus.MuddetiBitib;
                await _unitOfWork.Repository<QaraJetonBorcu>().YenileAsync(kb);
            }

            var borc = await _unitOfWork.Repository<QaraJetonBorcu>()
                .Query()
                .Where(x => x.IsciId == yeniJeton.IsciId
                    && x.Status == QaraJetonBorcuStatus.Gozleyir
                    && x.Il == cariIl)
                .OrderBy(x => x.YaradilmaTarixi)
                .FirstOrDefaultAsync();

            if (borc == null)
            {
                await _unitOfWork.YaddaSaxlaAsync();
                return;
            }

            var deyer = yeniTeyinat.SaatDeyeri;
            var kesilen = Math.Min(deyer, borc.QalanSaat);
            borc.QalanSaat -= kesilen;
            if (borc.QalanSaat <= 0) borc.Status = QaraJetonBorcuStatus.Odenildi;
            await _unitOfWork.Repository<QaraJetonBorcu>().YenileAsync(borc);

            // Yeni jeton borc qədər "yeyilir" — şəffaflıq üçün sıfır/azalmış
            // qalıqla, amma GÖRÜNƏN sətir olaraq saxlanır (istifadəçi qərarı).
            var qalanDeyer = deyer - kesilen;
            yeniJeton.QalanSaat = qalanDeyer;
            if (qalanDeyer <= 0) yeniJeton.Status = IsciJetonuStatus.IstifadeOlunub;
            yeniJeton.QaraJetonId = borc.QaraJetonId;
            yeniJeton.XerclenmeTarixi = DateTime.Now;
            await _unitOfWork.Repository<IsciJetonu>().YenileAsync(yeniJeton);

            await _unitOfWork.YaddaSaxlaAsync();
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
                    && !x.JetonTeyinati.Sistemli
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

                // ⚠️ 07.10.2026: Qara Jeton blok yoxlaması BURADAN SİLİNDİ — istifadəçi qərarı.
                // Əvvəl aktiv Qara Jeton varkən bütün jeton-redim sorğuları bloklanırdı
                // ("mükafat dondurulur"). Qara Jeton indi verilən AN dəyərini özü kəsdiyi
                // üçün (bax QaraJetonKesintisiTetbiqEtAsync) əlavə blok artıq mənasızdır —
                // işçi eyni cəzanı İKİ DƏFƏ (həm saat itkisi, həm redim qadağası) çəkməməlidir.
                // `AktivQaraJetonuVarmiAsync` metodu özü SİLİNMƏDİ (başqa yerlərdə/gələcəkdə
                // işlənə bilər), yalnız bu çağırış götürüldü.

                // Seçilmiş jetonları yoxla
                var jetonlar = await _unitOfWork.Repository<IsciJetonu>()
                    .Query()
                    .Include(x => x.JetonTeyinati)
                    .Where(x => dto.JetonIds.Contains(x.Id)
                        && x.IsciId == isciId
                        && x.Status == IsciJetonuStatus.Aktiv
                        && x.RedimTelebiId == null   // başqa gözləyən sorğuya bağlı olmasın (ikiqat rezerv qarşısı)
                        && x.JetonTeyinati.Nov == JetonNovu.Musbat
                        && !x.JetonTeyinati.Sistemli)
                    .ToListAsync();

                if (jetonlar.Count != dto.JetonIds.Count)
                    return Result.Fail("Seçilmiş jetonların bir hissəsi etibarsız və ya artıq başqa sorğudadır.");

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

                // İcazə müddəti (istənilən pəncərə) — bildiriş və cavab mesajında göstərilir.
                // Əsas məlumat PƏNCƏRƏdir; jeton (cemiSaat) yalnız büdcəni/ödənişi göstərir.
                decimal icazeSaat = dto.RedimNovu == RedimNovu.Icaze
                    ? (decimal)(dto.BitisSaati!.Value - dto.BaslamaSaati!.Value).TotalHours
                    : 0;

                // İlk növbədə Rəhbərə bildiriş gedir (HR yox — rəhbərdən sonra).
                string bildirisMesaj = dto.RedimNovu == RedimNovu.Icaze
                    ? $"İşçi {dto.IcazeTarixi!.Value:dd.MM.yyyy} tarixində " +
                      $"{dto.BaslamaSaati!.Value:hh\\:mm}–{dto.BitisSaati!.Value:hh\\:mm} ({icazeSaat:0.##} saat) icazə istəyir. " +
                      $"Jeton ödənişi: {cemiSaat:0.##} saat ({jetonlar.Count} jeton)."
                    : $"İşçi {cemiSaat:0.##} saatlıq maaş bonusu istəyir ({jetonlar.Count} jeton).";

                await _bildirisRouter.NotifyRolesAsync(
                    new[] { RoleNames.Rehber },
                    BildirisNovu.JetonVerildi,
                    "Yeni Jeton Sorğusu",
                    bildirisMesaj,
                    redirectUrl: "/HR/Jeton/Index?tab=redimler",
                    exceptIsciId: isciId);

                var amsalGoster = dto.RedimNovu == RedimNovu.Icaze ? saatAmsali : pulAmsali;
                var amsalMetn = amsalGoster != 1.00m ? $" (reytinq əmsalı: {amsalGoster}x)" : "";
                return Result.Ok(dto.RedimNovu == RedimNovu.Icaze
                    ? $"İcazə sorğusu göndərildi — {icazeSaat:0.##} saat (jeton ödənişi: {cemiSaat:0.##} saat{amsalMetn})."
                    : $"Maaşa əlavə sorğusu göndərildi: {cemiSaat:0.##} saat{amsalMetn}.");
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

                if (redim == null) return Result.Fail("Sorğu tapılmadı.");
                if (redim.Status != RedimStatus.Gozlenilir)
                    return Result.Fail("Bu sorğu artıq emal edilib.");
                if (redim.RehberTesdiq.HasValue)
                    return Result.Fail("Sorğu artıq rəhbər tərəfindən baxılıb.");

                redim.RehberTesdiq = true;
                redim.RehberUserId = rehberUserId;
                redim.RehberTesdiqTarixi = DateTime.Now;

                await _unitOfWork.Repository<JetonRedimTelebi>().YenileAsync(redim);
                await _unitOfWork.YaddaSaxlaAsync();

                // İcazə sorğusu → rəhbər təsdiqi YEKUNDUR (adi icazə saatı kimi tək mərhələ):
                // HR təsdiqini gözləmədən jetonu xərclə, İcazəni yarat və günü "İcazəli" et.
                if (redim.RedimNovu == RedimNovu.Icaze)
                    return await RedimTelebiTesdiqleAsync(redimId, rehberUserId);

                // Pul (maaş bonusu) → maliyyə nəzarəti üçün HR final təsdiqi qalır
                await _bildirisRouter.NotifyRolesAsync(
                    new[] { RoleNames.HR, RoleNames.Admin },
                    BildirisNovu.JetonVerildi,
                    "Rəhbər təsdiqindən keçdi",
                    $"{redim.Isci.Ad} {redim.Isci.Soyad}-in {redim.CemiSaat:0.##} saatlıq maaş bonusu sorğusu rəhbər təsdiqindən keçib.",
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

                if (redim == null) return Result.Fail("Sorğu tapılmadı.");
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
                    return Result.Fail("Sorğu tapılmadı.");

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
                    // Jeton icazəsində NaharNezereAlinmasin "işçi naharda işləyib" demək DEYİL —
                    // təqvim aralığını iş saatına çevirmək üçündür (8.75 təqvim → 8 iş saatı).
                    // İcazə tərəfindəki çıxılma isə SABİT nahar fasiləsidir (NaharCixilmaSaat),
                    // ona görə bayraq yalnız nahar pəncərəsi aralığın İÇİNƏ TAM düşəndə qoyulur —
                    // yalnız o halda sabit çıxılma jeton hesabı (IsSaatiHesabla, kəsişmə əsaslı)
                    // ilə eyni rəqəmi verir. Əks halda ekranda jetonla ödənilən saatdan az
                    // görünürdü (məs. 14:00–18:00 → 4 saat əvəzinə 3,25 saat).
                    var jetonPrm = await _unitOfWork.Repository<IsParametri>()
                        .Query().AsNoTracking().Where(x => !x.Silinib).FirstOrDefaultAsync();
                    var jNaharBas = jetonPrm?.NaharBaslamaSaati ?? new TimeSpan(13, 0, 0);
                    var jNaharBitis = jNaharBas + TimeSpan.FromMinutes(jetonPrm?.NaharMuddetDeqiqe ?? 45);
                    var jNaharIcerdedir = redim.BaslamaSaati.Value <= jNaharBas
                                       && redim.BitisSaati.Value >= jNaharBitis;

                    // TAM İŞ GÜNÜ: işçi bütün gün jetonla məzundur — ofisə ümumiyyətlə gəlmir,
                    // ona görə cihaz çıxış/qayıdışı GÖZLƏNİLMİR (aşağıda qeyd dərhal Tamamlandı
                    // açılır). Nahar aralığın içində olması ilə qarışdırma: 12:00–15:00 icazəsi
                    // də naharı əhatə edir, amma işçi həmin gün ofisdədir və cihaza vurur.
                    var jTamIsGunu = redim.BaslamaSaati.Value <= (jetonPrm?.StandartGirisVaxti ?? new TimeSpan(9, 0, 0))
                                  && redim.BitisSaati.Value >= (jetonPrm?.StandartCixisVaxti ?? new TimeSpan(17, 45, 0));

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
                        JetonOdenenSaat = redim.CemiSaat,
                        // Tam iş günü (nahar aralığın içindədir) → 8.75 təqvim yox, 8 iş saatı.
                        // Nahara toxunmayan qismən aralıqda çıxılma olmamalıdır.
                        NaharNezereAlinmasin = jNaharIcerdedir
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

                    // Dövriyyə izlənməsi üçün çıxış/qayıdış qeydi — adi icazə axını ilə eyni.
                    // (Bu olmadan jeton icazəsi İcazə Dövriyyəsi səhifəsinə düşmür və
                    // HR faktiki çıxış/qayıdışa düzəliş edə bilmirdi — real hadisə 05.08.2026.)
                    await _unitOfWork.Repository<IcazeCixisGiris>().YaratAsync(new IcazeCixisGiris
                    {
                        Icaze = icaze,
                        Birdefelik = false,
                        // Tam iş günü → gözləniləsi çıxış/qayıdış yoxdur, qeyd dərhal bağlanır
                        // (vaxtlar boş qalır — uydurma cihaz oxuması yazılmır; sayılan müddət
                        // plandan gəlir və jeton onu tam örtür). Qismən icazədə isə işçi
                        // ofisdədir və cihaza vurur → adi axın.
                        Status = jTamIsGunu
                            ? IcazeCixisGirisStatus.Tamamlandi
                            : IcazeCixisGirisStatus.Gozlenir
                    });

                    // Möhkəmlik: həmin günü davamiyyətdə dərhal "İcazəli" et (Qayib qalıb maaşdan
                    // kəsilməsin). Background marker yalnız qeydi olmayan günləri yazır — burada upsert.
                    // Yalnız qeyd yoxdursa və ya Qayib-dirsə dəyişir; işçi gəlib (Isde/Gecikme) qeydə toxunulmur.
                    var icazeGunu = redim.IcazeTarixi.Value.Date;
                    var dav = await _unitOfWork.Repository<Davamiyyet>()
                        .Query().FirstOrDefaultAsync(x => x.IsciId == redim.IsciId
                            && x.Tarix.Date == icazeGunu && !x.Silinib);
                    if (dav == null)
                    {
                        // ⚠️ YUMŞAQ SİLİNMİŞ SƏTİR — yuxarıdakı sorğu `!Silinib`
                        // filtri ilə işlədiyi üçün ləğv edilmiş məzuniyyətdən qalan
                        // sətri GÖRMÜR. `Davamiyyetler`-də unikal indeks (IsciId,
                        // Tarix) isə `Silinib`-i filtrləmir → üstünə INSERT etsək
                        // «An error occurred while saving the entity changes» alarıq.
                        // Kanonik həll: `MezuniyyetService.DavamiyyetUpsertAsync`
                        // (o, private-dır; qayda dəyişəndə ikisini birlikdə dəyiş).
                        var silinmis = await _unitOfWork.Repository<Davamiyyet>()
                            .SilinmisGetirAsync(x => x.IsciId == redim.IsciId
                                                  && x.Tarix.Date == icazeGunu);
                        if (silinmis != null)
                        {
                            silinmis.Silinib       = false;
                            silinmis.SilinmeTarixi = null;
                            silinmis.Status        = DavamiyyetStatus.Icazeli;
                            silinmis.MaasdanKes    = false;
                            silinmis.GirisVaxti    = null;
                            silinmis.CixisVaxti    = null;
                            await _unitOfWork.Repository<Davamiyyet>().YenileAsync(silinmis);
                        }
                        else
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

                return Result.Ok("Sorğu təsdiqləndi.");
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
                        && !x.JetonTeyinati.Sistemli // "36 Saat Hüququ" bura daxil deyil — yalnız Qara Jeton ona toxunur
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

        // Adi icazəyə jeton əvəzləşdirmə (FIFO tutulma) baş verdikdə GÖRÜNƏN redim qeydi yaradır.
        // Beləliklə işçi/HR "Xərcləmə Tarixçəsi"ndə jetonun niyə azaldığını adi redim kimi görür.
        // Qeyd: jeton onsuz da FIFO ilə tutulub — bu yalnız görünüş qeydidir (tək başına xərcləmir).
        public async Task<Result> JetonEvezlesdirmeQeydiAsync(int isciId, decimal jetonSaat, DateTime tarix, TimeSpan? baslama, TimeSpan? bitis, string qeyd)
        {
            try
            {
                if (jetonSaat <= 0) return Result.Ok();

                var redim = new JetonRedimTelebi
                {
                    IsciId = isciId,
                    RedimNovu = RedimNovu.Icaze,
                    CemiSaat = jetonSaat,
                    Status = RedimStatus.Tesdiqlendi,
                    IcazeTarixi = tarix,
                    BaslamaSaati = baslama,
                    BitisSaati = bitis,
                    RehberTesdiq = true,
                    RehberTesdiqTarixi = DateTime.Now,
                    TelabTarixi = DateTime.Now,
                    NeticeTarixi = DateTime.Now,
                    Qeyd = qeyd
                };

                await _unitOfWork.Repository<JetonRedimTelebi>().YaratAsync(redim);
                await _unitOfWork.YaddaSaxlaAsync();
                return Result.Ok();
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
                    return Result.Fail("Sorğu tapılmadı.");

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

                return Result.Ok("Sorğu rədd edildi.");
            }
            catch (Exception ex)
            {
                return Result.Fail($"Xəta: {ex.Message}");
            }
        }

        // İşçi öz sorğusunu təsdiqdən ƏVVƏL (yalnız Gozlenilir) ləğv edir → rezerv jetonlar geri qayıdır.
        public async Task<Result> RedimTelebiLegvEtAsync(int redimId, int isciId)
        {
            try
            {
                var redim = await _unitOfWork.Repository<JetonRedimTelebi>()
                    .Query()
                    .Include(x => x.XerclenenJetonlar)
                    .FirstOrDefaultAsync(x => x.Id == redimId);

                if (redim == null) return Result.Fail("Sorğu tapılmadı.");
                if (redim.IsciId != isciId) return Result.Fail("Bu sorğu sizə aid deyil.");
                if (redim.Status != RedimStatus.Gozlenilir)
                    return Result.Fail("Yalnız təsdiq gözləyən sorğu ləğv edilə bilər.");

                redim.Status = RedimStatus.Legv;
                redim.NeticeTarixi = DateTime.Now;

                // Rezerv olunmuş jetonları geri burax (Status Aktiv qalır, yalnız bağ açılır)
                foreach (var j in redim.XerclenenJetonlar)
                {
                    j.RedimTelebiId = null;
                    await _unitOfWork.Repository<IsciJetonu>().YenileAsync(j);
                }

                await _unitOfWork.Repository<JetonRedimTelebi>().YenileAsync(redim);
                await _unitOfWork.YaddaSaxlaAsync();

                return Result.Ok("Sorğunuz ləğv edildi, jetonlar geri qaytarıldı.");
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

                // Kataloqda görünmür, amma Id ilə birbaşa URL cəhdinə qarşı ikinci qat.
                if (teyinat.Sistemli)
                    return Result.Fail("Bu jeton növü sistem tərəfindən idarə olunur, redaktə edilə bilməz.");

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
            Aktivdir         = x.Aktivdir,
            Sistemli         = x.Sistemli
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
            // Menfi növdə dondurulmuş miqdar (MenfiMiqdar) üstünlük təşkil edir —
            // kataloqun sonradan dəyişən cari dəyəri tarixi "kəsilən saat"i
            // dəyişməsin (07.10.2026, KRİTİK). Köhnə (migrationdan əvvəlki)
            // sətirlərdə MenfiMiqdar NULL-dur, canlı dəyərə geri düşür.
            JetonSaatDeyeri = x.JetonTeyinati.Nov == JetonNovu.Menfi && x.MenfiMiqdar.HasValue
                ? x.MenfiMiqdar.Value
                : x.JetonTeyinati.SaatDeyeri,
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
            IcazeSaati = (x.RedimNovu == RedimNovu.Icaze && x.BaslamaSaati.HasValue && x.BitisSaati.HasValue)
                ? Math.Round((decimal)(x.BitisSaati.Value - x.BaslamaSaati.Value).TotalHours, 2)
                : 0,
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
