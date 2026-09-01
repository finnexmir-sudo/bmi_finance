using FinNex.Application.DTOs.HR.Ezamiyyet;
using FinNex.Application.Interfaces.Avtopark;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.HR
{
    public class EzamiyyetService : IEzamiyyetService
    {
        private readonly IUnitOfWork _uow;

        /// <summary>
        /// Avtopark bağlantısı (01.09.2026) — «bir forma, bir təsdiq».
        /// İşçi ezamiyyət formasında maşın seçirsə, rəhbər ezamiyyəti
        /// təsdiqləyəndə maşın müraciəti burada AVTOMATİK yaranır.
        ///
        /// Asılılıq TƏK İSTİQAMƏTLİDİR (Ezamiyyət → Avtopark). Avtopark
        /// tərəfi ezamiyyəti tanımır — əks istiqamətdə asılılıq əlavə etmə,
        /// DI dövrü yaranar.
        /// </summary>
        private readonly IMasinMuracietService _masin;

        public EzamiyyetService(IUnitOfWork uow, IMasinMuracietService masin)
        {
            _uow = uow;
            _masin = masin;
        }

        /// <summary>
        /// Ezamiyyətin maşın müraciəti üçün plan başlama vaxtı.
        ///
        /// Saatlı ezamiyyətdə işçinin yazdığı saat götürülür; TAM GÜN
        /// ezamiyyətdə (`BaslamaSaati == null`) iş gününün standart başlama
        /// saatı — çünki `MasinMuraciet.PlanBaslama` saatsız ola bilməz və
        /// `00:00` kassa ekranında yanlış görünərdi.
        /// </summary>
        private async Task<DateTime> MasinPlanBaslamaAsync(DateTime tarix, TimeSpan? saat)
        {
            if (saat.HasValue) return tarix.Date + saat.Value;

            var p = await _uow.Repository<IsParametri>().Query().AsNoTracking().FirstOrDefaultAsync();
            return tarix.Date + (p?.StandartGirisVaxti ?? new TimeSpan(9, 0, 0));
        }

        // ── Siyahı əməliyyatları ─────────────────────────────

        public async Task<IList<EzamiyyetMuracietListDto>> IsciMuracietleriAsync(int isciId)
        {
            var list = await _uow.Repository<EzamiyyetMuraciet>()
                .HamisiniGetirAsync(
                    x => x.IsciId == isciId && !x.Silinib,
                    include: q => q.Include(x => x.Mekan)
                                   .Include(x => x.Rehber)
                                   .Include(x => x.Masin),   // 01.09.2026 — maşın adı üçün
                    izlemeden: true);
            return list.OrderByDescending(x => x.BaslamaTarixi).Select(Map).ToList();
        }

        public async Task<IList<EzamiyyetMuracietListDto>> HamisiniGetirAsync(EzamiyyetFiltrDto? filtr = null)
        {
            var query = _uow.Repository<EzamiyyetMuraciet>()
                .Query()
                .AsNoTracking()
                .Where(x => !x.Silinib)
                .Include(x => x.Isci)
                    .ThenInclude(i => i.IsciTeyinatlari.Where(t => !t.Silinib))
                    .ThenInclude(t => t.Departament)
                .Include(x => x.Mekan)
                .Include(x => x.Rehber)
                .Include(x => x.Masin)      // 01.09.2026 — maşın adı üçün
                .AsQueryable();

            if (filtr != null)
            {
                if (filtr.IsciId.HasValue)
                    query = query.Where(x => x.IsciId == filtr.IsciId.Value);
                if (!string.IsNullOrWhiteSpace(filtr.IsciAd))
                    query = query.Where(x => x.Isci.TamAd.Contains(filtr.IsciAd));
                if (filtr.MekanId.HasValue)
                    query = query.Where(x => x.MekanId == filtr.MekanId.Value);
                if (filtr.Status.HasValue)
                    query = query.Where(x => x.Status == filtr.Status.Value);
                if (filtr.BaslangicTarix.HasValue)
                    query = query.Where(x => x.BaslamaTarixi.Date >= filtr.BaslangicTarix.Value.Date);
                if (filtr.SonTarix.HasValue)
                    query = query.Where(x => x.BitmeTarixi.Date <= filtr.SonTarix.Value.Date);
                if (filtr.DepartamentId.HasValue)
                    query = query.Where(x => x.Isci.IsciTeyinatlari
                        .Any(t => !t.Silinib && t.DepartamentId == filtr.DepartamentId.Value));
            }

            var list = await query.OrderByDescending(x => x.BaslamaTarixi).ToListAsync();
            return list.Select(Map).ToList();
        }

        // Rəhbər/HR üçün işçi-qruplu izləmə: cəmi səfər, gün, faktiki saat + detay siyahısı
        public async Task<IList<EzamiyyetIsciIzlemeDto>> GetIsciEzamIzlemeAsync(EzamiyyetFiltrDto? filtr = null)
        {
            // Cihaz çıxış/qayıdış vaxtlarını xam CihazOxuma punch-larından bərpa et
            // (İcazə/Görüşdə olan, ezamiyyətdə çatışmayan mexanizm).
            await CihazVaxtlariniBerpaEtAsync(filtr);

            var list = await HamisiniGetirAsync(filtr);

            return list
                .GroupBy(x => x.IsciId)
                .Select(g =>
                {
                    var ilk = g.First();
                    var ezamlar = g.OrderByDescending(x => x.BaslamaTarixi).ToList();
                    return new EzamiyyetIsciIzlemeDto
                    {
                        IsciId          = g.Key,
                        IsciAdSoyad     = ilk.IsciTamAd,
                        IsciSekil       = ilk.IsciSekil,
                        SobeAdi         = string.IsNullOrEmpty(ilk.IsciSobe) ? "-" : ilk.IsciSobe!,
                        VezifeAdi       = ilk.IsciVezife,
                        CemiEzam        = g.Count(),
                        TesdiqlenibSayi = g.Count(x => x.Status == EzamiyyetStatus.Tesdiqlendi),
                        GozlemeSayi     = g.Count(x => x.Status == EzamiyyetStatus.Gozleyir),
                        ReddSayi        = g.Count(x => x.Status == EzamiyyetStatus.Reddedildi),
                        CemiGun         = g.Sum(x => x.GunSayi),
                        FaktikiSaat     = ezamlar.Where(x => x.FaktikiSaat.HasValue).Sum(x => x.FaktikiSaat!.Value),
                        SonEzamTarixi   = g.Max(x => (DateTime?)x.BaslamaTarixi),
                        Ezamlar         = ezamlar
                    };
                })
                .OrderBy(x => x.SobeAdi).ThenBy(x => x.IsciAdSoyad)
                .ToList();
        }

        // ── Cihaz çıxış/qayıdışın xam CihazOxuma-dan bərpası ──────────
        // İcazə/Görüşdə mövcud olan, ezamiyyətdə çatışmayan mexanizm. Təsdiqlənmiş,
        // saatlı (qismən günlük) ezamiyyətin CihazCixisVaxti boşdursa — həmin işçinin
        // o günkü cihaz punch-larından pəncərə (BaslamaSaati−15dəq .. BitisSaati+15dəq)
        // içində İLK = çıxış, SON = qayıdış tutulub yazılır. Punch-lar artıq CihazOxuma-da
        // saxlandığı üçün təsdiqdən sonra gələn punch da, keçmiş səfərlər də bərpa olunur.
        private async Task CihazVaxtlariniBerpaEtAsync(EzamiyyetFiltrDto? filtr)
        {
            var bugun = DateTime.Now.Date;
            var q = _uow.Repository<EzamiyyetMuraciet>()
                .Query()
                .Where(x => !x.Silinib
                         && x.Status == EzamiyyetStatus.Tesdiqlendi
                         && x.CihazCixisVaxti == null
                         && x.BaslamaSaati != null                       // yalnız saatlı (qismən günlük)
                         && x.BaslamaTarixi.Date <= bugun
                         && x.BaslamaTarixi.Date >= bugun.AddDays(-92));  // yaxın 3 ay
            if (filtr?.IsciId != null)
                q = q.Where(x => x.IsciId == filtr.IsciId.Value);

            var berpaLazim = await q.ToListAsync();   // tracked — saxlamaq üçün
            if (berpaLazim.Count == 0) return;

            var isciIdler = berpaLazim.Select(x => x.IsciId).Distinct().ToList();
            var minT = berpaLazim.Min(x => x.BaslamaTarixi.Date);
            var maxT = berpaLazim.Max(x => x.BitmeTarixi.Date);

            var oxumalar = await _uow.Repository<CihazOxuma>()
                .Query().AsNoTracking()
                .Where(o => !o.Silinib && isciIdler.Contains(o.IsciId)
                         && o.Vaxt.Date >= minT && o.Vaxt.Date <= maxT)
                .Select(o => new { o.IsciId, o.Vaxt })
                .ToListAsync();

            var raw = oxumalar
                .GroupBy(o => $"{o.IsciId}|{o.Vaxt:yyyy-MM-dd}")
                .ToDictionary(g => g.Key, g => g.Select(p => p.Vaxt).OrderBy(v => v).ToList());

            bool deyisdi = false;
            foreach (var ez in berpaLazim)
            {
                var key = $"{ez.IsciId}|{ez.BaslamaTarixi:yyyy-MM-dd}";
                if (!raw.TryGetValue(key, out var punchlar)) continue;

                var alt = (ez.BaslamaSaati ?? TimeSpan.Zero) - TimeSpan.FromMinutes(15);
                var ust = (ez.BitisSaati ?? new TimeSpan(23, 59, 59)) + TimeSpan.FromMinutes(15);
                var pencere = punchlar.Where(v => v.TimeOfDay >= alt && v.TimeOfDay <= ust).ToList();
                if (pencere.Count == 0) continue;

                ez.CihazCixisVaxti = pencere[0];                                    // ilk = çıxış
                if (pencere.Count > 1 && pencere[^1] > pencere[0].AddMinutes(5))
                    ez.CihazQayidisVaxti = pencere[^1];                             // son = qayıdış
                ez.YenilenmeTarixi = DateTime.Now;
                await _uow.Repository<EzamiyyetMuraciet>().YenileAsync(ez);
                deyisdi = true;
            }

            if (deyisdi)
                await _uow.YaddaSaxlaAsync();
        }

        public async Task<IList<EzamiyyetMuracietListDto>> GozleyenlerAsync()
        {
            var list = await _uow.Repository<EzamiyyetMuraciet>()
                .HamisiniGetirAsync(
                    x => !x.Silinib && x.Status == EzamiyyetStatus.Gozleyir,
                    include: q => q.Include(x => x.Isci)
                                       .ThenInclude(i => i.IsciTeyinatlari.Where(t => !t.Silinib))
                                       .ThenInclude(t => t.Departament)
                                   .Include(x => x.Mekan)
                                   .Include(x => x.Masin),   // 01.09.2026 — maşın adı üçün
                    izlemeden: true);
            return list.OrderBy(x => x.BaslamaTarixi).Select(Map).ToList();
        }

        public async Task<EzamiyyetMuracietListDto?> DetayAsync(int id)
        {
            var entity = await _uow.Repository<EzamiyyetMuraciet>()
                .Query()
                .AsNoTracking()
                .Include(x => x.Isci)
                    .ThenInclude(i => i.IsciTeyinatlari.Where(t => !t.Silinib))
                    .ThenInclude(t => t.Departament)
                .Include(x => x.Mekan)
                .Include(x => x.Rehber)
                .Include(x => x.Masin)      // 01.09.2026 — maşın adı üçün
                .FirstOrDefaultAsync(x => x.Id == id && !x.Silinib);
            return entity == null ? null : Map(entity);
        }

        // ── Yarat ────────────────────────────────────────────

        public async Task<(bool ok, string? error, int id)> YaratAsync(
            int isciId,
            EzamiyyetMuracietCreateDto dto,
            string dmsRoot)
        {
            if (string.IsNullOrWhiteSpace(dto.Baslig))
                return (false, "Başlıq boş ola bilməz.", 0);
            if (dto.BaslamaTarixi.Date > dto.BitmeTarixi.Date)
                return (false, "Başlama tarixi bitmə tarixindən böyük ola bilməz.", 0);

            // Maşın seçilibsə YAZMADAN ƏVVƏL yoxlanılır — təsdiq anında yox.
            // Yoxsa işçi formanı göndərəndə hər şey qaydasında görünər, problem
            // günlər sonra rəhbərin ekranında üzə çıxardı.
            if (dto.MasinId.HasValue && dto.MasinId.Value > 0)
            {
                var masinYoxla = await _masin.MasinSecimiYoxlaAsync(dto.MasinId.Value);
                if (!masinYoxla.Success)
                    return (false, masinYoxla.Message, 0);
            }

            int mekanId;
            if (dto.MekanId.HasValue && dto.MekanId.Value > 0)
            {
                mekanId = dto.MekanId.Value;
            }
            else if (!string.IsNullOrWhiteSpace(dto.YeniMekanAd))
            {
                var yeni = await YeniMekanYaratAsync(dto.YeniMekanAd.Trim());
                mekanId = yeni!.Id;
            }
            else
            {
                return (false, "Məkan seçin və ya yeni məkan adı daxil edin.", 0);
            }

            // Sənəd
            string? senedYolu = null, senedAd = null;
            if (dto.Sened != null && dto.Sened.Length > 0)
            {
                var dir = Path.Combine(dmsRoot, "ezamiyyet");
                Directory.CreateDirectory(dir);
                var ext = Path.GetExtension(dto.Sened.FileName);
                var fn = $"{Guid.NewGuid()}{ext}";
                await using var fs = new FileStream(Path.Combine(dir, fn), FileMode.Create);
                await dto.Sened.CopyToAsync(fs);
                senedYolu = $"ezamiyyet/{fn}";
                senedAd   = dto.Sened.FileName;
            }

            // Saat parse
            TimeSpan? baslamaSaati = ParseSaat(dto.BaslamaSaati);
            TimeSpan? bitisSaati   = ParseSaat(dto.BitisSaati);

            var entity = new EzamiyyetMuraciet
            {
                IsciId        = isciId,
                Baslig        = dto.Baslig.Trim(),
                MekanId       = mekanId,
                BaslamaTarixi = dto.BaslamaTarixi.Date,
                BitmeTarixi   = dto.BitmeTarixi.Date,
                BaslamaSaati  = baslamaSaati,
                BitisSaati    = bitisSaati,
                SenedYolu     = senedYolu,
                SenedAd       = senedAd,
                Qeyd          = dto.Qeyd?.Trim(),
                // «İstənilən» maşın — verilmiş maşın DEYİL (bax entity sənədi).
                MasinId       = (dto.MasinId.HasValue && dto.MasinId.Value > 0) ? dto.MasinId : null,
                Status        = EzamiyyetStatus.Gozleyir
            };

            await _uow.Repository<EzamiyyetMuraciet>().YaratAsync(entity);
            await _uow.YaddaSaxlaAsync();
            return (true, null, entity.Id);
        }

        // ── Rəhbər təsdiq ────────────────────────────────────

        public async Task<(bool ok, string? error)> RehberTesdiqAsync(
            int id, bool tesdiq, string? qeyd, int rehberId)
        {
            var entity = await _uow.Repository<EzamiyyetMuraciet>()
                .Query()
                .FirstOrDefaultAsync(x => x.Id == id && !x.Silinib);
            if (entity == null)
                return (false, "Müraciət tapılmadı.");
            if (entity.Status != EzamiyyetStatus.Gozleyir)
                return (false, "Müraciət artıq cavablanıb.");

            // Rollback üçün əvvəlki hal saxlanılır — bax aşağıdakı `Geriye()`.
            var evvStatus  = entity.Status;
            var evvTesdiq  = entity.RehberTesdiq;
            var evvRehber  = entity.RehberId;
            var evvTarix   = entity.RehberTesdiqTarixi;
            var evvQeyd    = entity.RehberQeydi;

            entity.RehberTesdiq       = tesdiq;
            entity.RehberId           = rehberId;
            entity.RehberTesdiqTarixi = DateTime.Now;
            entity.RehberQeydi        = qeyd;
            entity.Status             = tesdiq ? EzamiyyetStatus.Tesdiqlendi : EzamiyyetStatus.Reddedildi;

            // ⚠️ TRANZAKSİYA ROLLBACK-İ YADDAŞI GERİ QAYTARMIR.
            // `entity` izlənilir (tracked) və dəyişdirilmiş vəziyyətdə qalır;
            // eyni sorğuda sonradan istənilən `YaddaSaxlaAsync` (məs. bildiriş
            // yazılışı) onu BAZAYA YENİDƏN yazardı — ezamiyyət «təsdiqlənmiş»
            // görünər, maşın müraciəti isə olmazdı. Ona görə əl ilə qaytarırıq.
            void Geriye()
            {
                entity.Status             = evvStatus;
                entity.RehberTesdiq       = evvTesdiq;
                entity.RehberId           = evvRehber;
                entity.RehberTesdiqTarixi = evvTarix;
                entity.RehberQeydi        = evvQeyd;
            }

            // ── Maşın istənməyibsə köhnə yol (01.09.2026-dan əvvəlki kimi) ──
            if (!tesdiq || !entity.MasinId.HasValue)
            {
                await _uow.Repository<EzamiyyetMuraciet>().YenileAsync(entity);
                await _uow.YaddaSaxlaAsync();
                return (true, null);
            }

            // ── Maşınlı ezamiyyət — İKİSİ BİR YERDƏ YAZILIR ─────────────────
            // Tranzaksiya MƏCBURİDİR: ezamiyyət «təsdiqləndi» yazılıb maşın
            // müraciəti yazılmasa, işçi təsdiq görər, kassada isə heç nə
            // olmaz — və bu, heç bir yerdə iz qoymaz.
            var mekanAdi = await _uow.Repository<EzamiyyetMekan>().Query().AsNoTracking()
                .Where(m => m.Id == entity.MekanId)
                .Select(m => m.Ad)
                .FirstOrDefaultAsync();

            var planBaslama = await MasinPlanBaslamaAsync(entity.BaslamaTarixi, entity.BaslamaSaati);

            await using var tran = await _uow.BeginTransactionAsync();
            try
            {
                await _uow.Repository<EzamiyyetMuraciet>().YenileAsync(entity);
                await _uow.YaddaSaxlaAsync();

                var netice = await _masin.EzamiyyetdenYaratAsync(
                    ezamiyyetId: entity.Id,
                    masinId:     entity.MasinId.Value,
                    isciId:      entity.IsciId,
                    planBaslama: planBaslama,
                    meqsed:      entity.Baslig,
                    marsrut:     mekanAdi,
                    rehberIsciId: rehberId,
                    userId:      rehberId);

                if (!netice.Success)
                {
                    await tran.RollbackAsync();
                    Geriye();
                    // Ezamiyyət də təsdiqlənmir — rəhbər səbəbi görüb qərar versin
                    // (məs. maşın təmirə düşübsə başqa maşın seçilməlidir).
                    return (false, $"Ezamiyyət təsdiqlənmədi — maşın müraciəti yaradıla bilmədi: {netice.Message}");
                }

                await tran.CommitAsync();
                return (true, null);
            }
            catch
            {
                await tran.RollbackAsync();
                Geriye();
                throw;
            }
        }

        // ── Ləğv et ──────────────────────────────────────────

        public async Task<(bool ok, string? error)> LegvEtAsync(int id, int isciId)
        {
            var entity = await _uow.Repository<EzamiyyetMuraciet>()
                .Query()
                .FirstOrDefaultAsync(x => x.Id == id && x.IsciId == isciId && !x.Silinib);
            if (entity == null)
                return (false, "Müraciət tapılmadı.");
            if (entity.Status == EzamiyyetStatus.Tesdiqlendi &&
                entity.BaslamaTarixi.Date <= DateTime.Today)
                return (false, "Başlanmış ezamiyyəti ləğv etmək mümkün deyil.");

            entity.Status  = EzamiyyetStatus.Legvedildi;
            entity.Silinib = true;
            entity.SilinmeTarixi = DateTime.Now;
            await _uow.Repository<EzamiyyetMuraciet>().YenileAsync(entity);
            await _uow.YaddaSaxlaAsync();

            // Bağlı maşın müraciəti də bağlanır — yoxsa kassa olmayan səfər üçün
            // açar gözləyər. Açar ARTIQ VERİLİBSƏ toxunulmur (aşağıya bax).
            var masinBagli = await _masin.EzamiyyetLegvindeMasiniLegvEtAsync(entity.Id, isciId);
            if (!masinBagli)
                return (true, "Ezamiyyət ləğv edildi, AMMA maşının açarı artıq verilib — " +
                              "maşını qaytarıb kassaya «Gəldi» qeyd etdirin.");

            return (true, null);
        }

        // Rəhbər/HR — təsdiqlənmiş ezamiyyəti ləğv edir (sahibə bağlı deyil).
        // Səbəb məcburi; yalnız bu gün/gələcək (bitmiş ezamiyyət ləğv olunmur).
        public async Task<(bool ok, string? error)> RehberHrLegvEtAsync(int id, int legvEdenIsciId, string sebeb)
        {
            if (string.IsNullOrWhiteSpace(sebeb))
                return (false, "Ləğv səbəbi mütləqdir.");

            var entity = await _uow.Repository<EzamiyyetMuraciet>()
                .Query()
                .FirstOrDefaultAsync(x => x.Id == id && !x.Silinib);
            if (entity == null)
                return (false, "Müraciət tapılmadı.");
            if (entity.Status != EzamiyyetStatus.Tesdiqlendi)
                return (false, "Yalnız təsdiqlənmiş ezamiyyət ləğv edilə bilər.");
            if (entity.BitmeTarixi.Date < DateTime.Today)
                return (false, "Keçmiş (bitmiş) ezamiyyəti ləğv etmək mümkün deyil.");

            entity.Status          = EzamiyyetStatus.Legvedildi;
            entity.Silinib         = true;
            entity.SilinmeTarixi   = DateTime.Now;
            entity.YenilenmeTarixi = DateTime.Now;
            entity.GeriDonusQeydi  = $"Ləğv (Rəhbər/HR): {sebeb.Trim()}";
            await _uow.Repository<EzamiyyetMuraciet>().YenileAsync(entity);
            await _uow.YaddaSaxlaAsync();

            // İşçinin öz ləğvi ilə EYNİ qayda — ləğvin İKİ giriş nöqtəsi var,
            // birini unutsaq xəta yalnız o yolda təzahür edər (CLAUDE.md).
            var masinBagli = await _masin.EzamiyyetLegvindeMasiniLegvEtAsync(entity.Id, legvEdenIsciId);
            if (!masinBagli)
                return (true, "Ezamiyyət ləğv edildi, AMMA maşının açarı artıq verilib — " +
                              "kassa «Gəldi» qeyd etməlidir.");

            return (true, null);
        }

        // ── Geri dönüş notu ───────────────────────────────────

        public async Task<(bool ok, string? error)> GeriQeydElavEtAsync(int id, int isciId, string? qeyd)
        {
            var entity = await _uow.Repository<EzamiyyetMuraciet>()
                .Query()
                .FirstOrDefaultAsync(x => x.Id == id && x.IsciId == isciId && !x.Silinib);
            if (entity == null)
                return (false, "Müraciət tapılmadı.");
            if (entity.Status != EzamiyyetStatus.Tesdiqlendi)
                return (false, "Yalnız təsdiqlənmiş ezamiyyətlərə not əlavə edilə bilər.");
            if (entity.BitmeTarixi.Date > DateTime.Today)
                return (false, "Ezamiyyət hələ bitməyib.");

            entity.GeriDonusQeydi  = qeyd?.Trim();
            entity.YenilenmeTarixi = DateTime.Now;
            await _uow.Repository<EzamiyyetMuraciet>().YenileAsync(entity);
            await _uow.YaddaSaxlaAsync();
            return (true, null);
        }

        // ── HR əl ilə cihaz çıxış/qayıdış düzəlişi ────────────
        // İnsan faktoru: işçi qayıdışını cihazda qeyd etmədikdə və ya cihaz
        // səhər icazəsini yanlış oxuduqda HR çıxış/qayıdış vaxtını əl ilə düzəldir.
        // Boş (null) ötürülən sahə təmizlənir.
        public async Task<(bool ok, string? error)> CihazQayidisDuzeltAsync(
            int id, DateTime? cixisVaxt, DateTime? qayidisVaxt)
        {
            var entity = await _uow.Repository<EzamiyyetMuraciet>()
                .Query()
                .FirstOrDefaultAsync(x => x.Id == id && !x.Silinib);
            if (entity == null)
                return (false, "Müraciət tapılmadı.");
            if (entity.Status != EzamiyyetStatus.Tesdiqlendi)
                return (false, "Yalnız təsdiqlənmiş ezamiyyətin cihaz vaxtı düzəldilə bilər.");
            if (cixisVaxt.HasValue && qayidisVaxt.HasValue && qayidisVaxt.Value < cixisVaxt.Value)
                return (false, "Qayıdış vaxtı çıxış vaxtından əvvəl ola bilməz.");

            entity.CihazCixisVaxti   = cixisVaxt;
            entity.CihazQayidisVaxti = qayidisVaxt;
            entity.YenilenmeTarixi   = DateTime.Now;

            await _uow.Repository<EzamiyyetMuraciet>().YenileAsync(entity);
            await _uow.YaddaSaxlaAsync();
            return (true, null);
        }

        // ── Məkan ─────────────────────────────────────────────

        public async Task<IList<EzamiyyetMekanListDto>> MekanlarAsync()
        {
            var mekanlar = await _uow.Repository<EzamiyyetMekan>()
                .Query()
                .AsNoTracking()
                .Where(x => !x.Silinib && x.Aktiv)
                .Select(x => new EzamiyyetMekanListDto
                {
                    Id   = x.Id,
                    Ad   = x.Ad,
                    Aktiv = x.Aktiv,
                    Sayi = x.Muracietler.Count(m => !m.Silinib)
                })
                .OrderBy(x => x.Ad)
                .ToListAsync();
            return mekanlar;
        }

        public async Task<EzamiyyetMekan?> YeniMekanYaratAsync(string ad)
        {
            var movcud = await _uow.Repository<EzamiyyetMekan>()
                .Query()
                .FirstOrDefaultAsync(x => x.Ad.ToLower() == ad.ToLower() && !x.Silinib);
            if (movcud != null) return movcud;

            var yeni = new EzamiyyetMekan { Ad = ad, Aktiv = true };
            await _uow.Repository<EzamiyyetMekan>().YaratAsync(yeni);
            await _uow.YaddaSaxlaAsync();
            return yeni;
        }

        // ── ADMS inteqrasiyası ────────────────────────────────

        public async Task<EzamiyyetMuraciet?> BugunAktivEzamiyyetAsync(int isciId, DateTime tarix)
        {
            return await _uow.Repository<EzamiyyetMuraciet>()
                .Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.IsciId == isciId &&
                    !x.Silinib &&
                    x.Status == EzamiyyetStatus.Tesdiqlendi &&
                    x.BaslamaTarixi.Date <= tarix.Date &&
                    x.BitmeTarixi.Date   >= tarix.Date);
        }

        // ── Statistika ────────────────────────────────────────

        public async Task<IList<EzamiyyetStatistikDto>> StatistikaAsync(EzamiyyetFiltrDto filtr)
        {
            var query = _uow.Repository<EzamiyyetMuraciet>()
                .Query()
                .AsNoTracking()
                .Where(x => !x.Silinib)
                .Include(x => x.Isci)
                    .ThenInclude(i => i.IsciTeyinatlari.Where(t => !t.Silinib))
                    .ThenInclude(t => t.Departament)
                .Include(x => x.Mekan)
                .AsQueryable();

            if (filtr.BaslangicTarix.HasValue)
                query = query.Where(x => x.BaslamaTarixi.Date >= filtr.BaslangicTarix.Value.Date);
            if (filtr.SonTarix.HasValue)
                query = query.Where(x => x.BitmeTarixi.Date <= filtr.SonTarix.Value.Date);
            if (filtr.DepartamentId.HasValue)
                query = query.Where(x => x.Isci.IsciTeyinatlari
                    .Any(t => !t.Silinib && t.DepartamentId == filtr.DepartamentId.Value));

            var list = await query.ToListAsync();

            return list
                .GroupBy(x => x.IsciId)
                .Select(g =>
                {
                    var isci = g.First().Isci;
                    var dep  = isci.IsciTeyinatlari.FirstOrDefault()?.Departament?.Ad;
                    var enCoxMekan = g
                        .GroupBy(x => x.Mekan.Ad)
                        .OrderByDescending(mg => mg.Count())
                        .Select(mg => mg.Key)
                        .FirstOrDefault();

                    return new EzamiyyetStatistikDto
                    {
                        IsciId       = g.Key,
                        IsciTamAd    = isci.TamAd,
                        Departament  = dep,
                        CemiMuraciet = g.Count(),
                        Tesdiqlendi  = g.Count(x => x.Status == EzamiyyetStatus.Tesdiqlendi),
                        Reddedildi   = g.Count(x => x.Status == EzamiyyetStatus.Reddedildi),
                        Gozleyir     = g.Count(x => x.Status == EzamiyyetStatus.Gozleyir),
                        CemiGun      = g.Where(x => x.Status == EzamiyyetStatus.Tesdiqlendi)
                                        .Sum(x => (x.BitmeTarixi.Date - x.BaslamaTarixi.Date).Days + 1),
                        EnCoxMekan   = enCoxMekan
                    };
                })
                .OrderByDescending(x => x.CemiGun)
                .ToList();
        }

        // ── Helpers ───────────────────────────────────────────

        private static TimeSpan? ParseSaat(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            if (!TimeSpan.TryParse(s.Trim(), out var ts)) return null;
            // Yalnız gündaxili saat (00:00–23:59). Kolonsuz "9" kimi dəyər TimeSpan-da
            // 9 GÜN kimi parse olunduğundan, 1 gündən böyük/bərabər nəticələri rədd edirik.
            if (ts < TimeSpan.Zero || ts >= TimeSpan.FromDays(1)) return null;
            return ts;
        }

        private static EzamiyyetMuracietListDto Map(EzamiyyetMuraciet x)
        {
            var teyinat = x.Isci?.IsciTeyinatlari?.FirstOrDefault(t => !t.Silinib);
            return new EzamiyyetMuracietListDto
            {
                Id                  = x.Id,
                IsciId              = x.IsciId,
                IsciTamAd           = x.Isci?.TamAd ?? "",
                IsciVezife          = teyinat?.Vezife?.Ad,
                IsciSobe            = teyinat?.Departament?.Ad,
                Baslig              = x.Baslig,
                MekanId             = x.MekanId,
                MekanAd             = x.Mekan?.Ad ?? "",
                BaslamaTarixi       = x.BaslamaTarixi,
                BitmeTarixi         = x.BitmeTarixi,
                BaslamaSaati        = x.BaslamaSaati,
                BitisSaati          = x.BitisSaati,
                SenedYolu           = x.SenedYolu,
                SenedAd             = x.SenedAd,
                Qeyd                = x.Qeyd,
                Status              = x.Status,
                RehberTesdiq        = x.RehberTesdiq,
                RehberTamAd         = x.Rehber?.TamAd,
                RehberTesdiqTarixi  = x.RehberTesdiqTarixi,
                RehberQeydi         = x.RehberQeydi,
                GeriDonusQeydi      = x.GeriDonusQeydi,
                CihazCixisVaxti    = x.CihazCixisVaxti,
                CihazQayidisVaxti  = x.CihazQayidisVaxti,
                YaradilmaTarixi    = x.YaradilmaTarixi,
                MasinId            = x.MasinId,
                // Naviqasiya Include edilməyibsə `null` qalır — ad yoxdursa
                // ekran yalnız «maşınla» yazır, sətir sınmır.
                MasinAdi           = x.Masin == null
                    ? null
                    : ($"{x.Masin.DovletNomresi} · {x.Masin.Marka} {x.Masin.Model}").Trim()
            };
        }
    }
}
