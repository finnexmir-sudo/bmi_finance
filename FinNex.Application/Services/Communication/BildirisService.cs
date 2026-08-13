using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Communication;
using FinNex.Application.Interfaces.Communication;
using FinNex.Domain.Entities.Communication;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;

namespace FinNex.Application.Services.Communication
{
    public class BildirisService : IBildirisService
    {
        private readonly IUnitOfWork              _unitOfWork;
        private readonly IDesktopBildirisService  _desktop;

        // Dublikat qoruma pəncərəsi (saniyə) — bax: YaratAsync, addım 0.
        // Dar saxlanılır ki, eyni müraciətin sonrakı mərhələ bildirişləri (təsdiq,
        // imtina, ödəniş — dəqiqələr sonra gəlir) heç vaxt bloklanmasın.
        private const int DublikatPenceresiSaniye = 15;

        public BildirisService(IUnitOfWork unitOfWork, IDesktopBildirisService desktop)
        {
            _unitOfWork = unitOfWork;
            _desktop    = desktop;
        }

        // Silinmiş (ləğv edilmiş) məzuniyyətin TƏSDİQ GÖZLƏYƏN bildirişlərini süzür.
        //
        // NİYƏ: işçi müraciətini ləğv edəndə `Mezuniyyet` YUMŞAQ silinir, amma ona
        // bağlı `Bildiris` sətirləri yerində qalırdı. Nəticədə rəhbərin siyahısında
        // artıq mövcud olmayan müraciətin "təsdiq et" bildirişi qalır və o, müraciəti
        // "iki dəfə gəlmiş" kimi görürdü (13.08.2026 hadisəsi).
        //
        // Ləğv anında bu bildirişlər onsuz da silinir (MezuniyyetBildirisleriniSilAsync);
        // buradakı süzgəc KEÇMİŞDƏ yaranmış qalıqlar üçün ikinci qatdır.
        //
        // ⚠️ YALNIZ `MezuniyyetMuraciet` süzülür — bu, "sənə iş gəlib, təsdiq et"
        // bildirişidir və müraciət yoxdursa mənasızdır. QALAN NÖVLƏRƏ TOXUNULMUR:
        // `MezuniyyetImtina` (HR ləğv etdi / Mühasibə "ödənişi icra etməyin"),
        // `MezuniyyetTesdiq` (təsdiq, tarix dəyişikliyi) — bunlar məhz məzuniyyət
        // silinəndən SONRA yaradılır və `MezuniyyetId`-si silinmiş qeydə baxır.
        // Növ şərti olmasaydı bu süzgəc onları da gizlədərdi: işçi "məzuniyyətiniz
        // ləğv edildi" xəbərini, Mühasib isə "ödənişi icra etməyin" xəbərdarlığını
        // heç vaxt görməzdi.
        //
        // DİQQƏT: siyahı və SAY eyni süzgəcdən keçməlidir — biri süzüb, o biri
        // saysa "3 bildiriş var" yazar, açanda 2 görünər (CLAUDE.md: say = siyahı).
        private async Task<IList<Bildiris>> DiriBildirislerAsync(IList<Bildiris> list)
        {
            var mezIdler = list
                .Where(x => x.Nov == BildirisNovu.MezuniyyetMuraciet && x.MezuniyyetId.HasValue)
                .Select(x => x.MezuniyyetId!.Value)
                .Distinct()
                .ToList();

            if (mezIdler.Count == 0) return list;

            // HamisiniGetirAsync onsuz da `!Silinib` süzgəcini tətbiq edir →
            // qayıdanlar yalnız DİRİ məzuniyyətlərdir.
            var diriIdler = (await _unitOfWork.Repository<Mezuniyyet>()
                    .HamisiniGetirAsync(x => mezIdler.Contains(x.Id), izlemeden: true))
                .Select(x => x.Id)
                .ToHashSet();

            return list
                .Where(x => x.Nov != BildirisNovu.MezuniyyetMuraciet
                         || !x.MezuniyyetId.HasValue
                         || diriIdler.Contains(x.MezuniyyetId.Value))
                .ToList();
        }

        public async Task<Result<IList<BildirisDto>>> GetIscibildirisleriAsync(int isciId)
        {
            try
            {
                var hamisi = await _unitOfWork.Repository<Bildiris>()
                    .HamisiniGetirAsync(
                        predicate: x => x.IsciId == isciId && !x.Silinib,
                        izlemeden: true);

                var list = await DiriBildirislerAsync(hamisi);

                var dtos = list
                    .OrderByDescending(x => x.YaradilmaTarixi)
                    .Select(b => new BildirisDto
                    {
                        Id              = b.Id,
                        Nov             = b.Nov,
                        Bashliq         = b.Bashliq,
                        Metn            = b.Metn,
                        Oxunub          = b.Oxunub,
                        OxunmaTarixi    = b.OxunmaTarixi,
                        YaradilmaTarixi = b.YaradilmaTarixi,
                        RedirectUrl     = b.RedirectUrl,
                        MezuniyyetId    = b.MezuniyyetId,
                        IcazeId         = b.IcazeId,
                        MesajId         = b.MesajId
                    }).ToList();

                return Result<IList<BildirisDto>>.Ok(dtos);
            }
            catch (Exception ex)
            {
                return Result<IList<BildirisDto>>.Fail($"Xəta: {ex.Message}");
            }
        }

        public async Task<Result> OxunduIsareEtAsync(int bildirisId, int isciId)
        {
            var b = await _unitOfWork.Repository<Bildiris>()
                .GetirAsync(x => x.Id == bildirisId && x.IsciId == isciId);

            if (b == null) return Result.Fail("Tapılmadı.");

            b.Oxunub       = true;
            b.OxunmaTarixi = DateTime.Now;

            await _unitOfWork.Repository<Bildiris>().YenileAsync(b);
            await _unitOfWork.YaddaSaxlaAsync();
            return Result.Ok();
        }

        public async Task<Result> HamisiniOxunduIsareEtAsync(int isciId)
        {
            var list = await _unitOfWork.Repository<Bildiris>()
                .HamisiniGetirAsync(x => x.IsciId == isciId && !x.Oxunub && !x.Silinib);

            foreach (var b in list)
            {
                b.Oxunub       = true;
                b.OxunmaTarixi = DateTime.Now;
                await _unitOfWork.Repository<Bildiris>().YenileAsync(b);
            }

            await _unitOfWork.YaddaSaxlaAsync();
            return Result.Ok();
        }

        public async Task<Result<int>> OxunmamisSayiAsync(int isciId)
        {
            var hamisi = await _unitOfWork.Repository<Bildiris>()
                .HamisiniGetirAsync(
                    predicate: x => x.IsciId == isciId && !x.Oxunub && !x.Silinib,
                    izlemeden: true);

            // Siyahı ilə EYNİ süzgəc — say ≠ siyahı olmasın (bax: DiriBildirislerAsync)
            var sayi = (await DiriBildirislerAsync(hamisi)).Count;

            return Result<int>.Ok(sayi);
        }

        public async Task<Result> YaratAsync(
            int isciId, BildirisNovu nov,
            string bashliq, string metn, string? redirectUrl = null,
            int? mezuniyyetId = null, int? icazeId = null, int? mesajId = null)
        {
            try
            {
                // 0. DUBLİKAT QORUMASI
                //
                // Eyni alıcıya, eyni hadisə üçün, saniyələr içində ikinci bildiriş
                // həmişə səhvdir — istifadəçi onu "iki dəfə gəldi" kimi görür.
                // Real hadisə (13.08.2026): rəhbərə eyni məzuniyyət müraciəti üçün
                // 3,3 ms fərqlə iki eyni bildiriş düşdü.
                //
                // Pəncərə QƏSDƏN dardır (DublikatPenceresiSaniye): eyni müraciətin
                // sonrakı mərhələləri (təsdiq, imtina, ödəniş) dəqiqələr/saatlar
                // sonra gəlir və HEÇ VAXT bloklanmır — bazadakı real təkrarlar
                // 15 və 35 dəqiqə aralı idi, onlar bu qorumadan təsirlənmir.
                //
                // Açar QƏSDƏN geniş tutulub — alıcı + növ + başlıq + MƏTN + bağlı
                // olduğu qeyd (məzuniyyət/icazə/mesaj). Mətn də daxildir ki, eyni
                // başlıqlı, amma FƏRQLİ hadisələr (məs. eyni anda təyin edilən iki
                // ayrı tapşırıq — "Yeni tapşırıq" başlığı eynidir, mətn fərqlidir)
                // səhvən bir-birini bloklamasın. Bloklanan yalnız hərfbəhərf eyni
                // bildirişdir.
                var hedd = DateTime.Now.AddSeconds(-DublikatPenceresiSaniye);
                var movcuddur = await _unitOfWork.Repository<Bildiris>().MovcuddurmuAsync(x =>
                    x.IsciId       == isciId &&
                    x.Nov          == nov &&
                    x.Bashliq      == bashliq &&
                    x.Metn         == metn &&
                    x.MezuniyyetId == mezuniyyetId &&
                    x.IcazeId      == icazeId &&
                    x.MesajId      == mesajId &&
                    !x.Silinib &&
                    x.YaradilmaTarixi >= hedd);

                if (movcuddur)
                    return Result.Ok();   // artıq göndərilib — təkrar yazılmır

                // 1. Verilənlər bazına yaz
                var entity = new Bildiris
                {
                    IsciId       = isciId,
                    Nov          = nov,
                    Bashliq      = bashliq,
                    Metn         = metn,
                    RedirectUrl  = redirectUrl,
                    MezuniyyetId = mezuniyyetId,
                    IcazeId      = icazeId,
                    MesajId      = mesajId
                };

                await _unitOfWork.Repository<Bildiris>().YaratAsync(entity);
                await _unitOfWork.YaddaSaxlaAsync();

                // 2. Desktop agentə anlıq push (fire-and-forget).
                //    Xəta DB əməliyyatını etkiləməsin dəyə await etmirik.
                _ = _desktop.PushAsync(isciId, bashliq, metn, redirectUrl, nov);

                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Fail($"Xəta: {ex.Message}");
            }
        }

        public async Task<Result<int>> MezuniyyetBildirisleriniSilAsync(int mezuniyyetId)
        {
            try
            {
                if (mezuniyyetId <= 0) return Result<int>.Ok(0);

                // İki məhdudiyyət — hər ikisi qəsdən:
                //  • yalnız `MezuniyyetMuraciet` — yəni "təsdiq et" iş bildirişi.
                //    Təsdiq/imtina/ödəniş bildirişləri xəbərdir, silinmir
                //    (DiriBildirislerAsync ilə eyni qayda);
                //  • yalnız OXUNMAMIŞ — oxunmuş bildiriş baş vermiş hadisənin
                //    tarixçəsidir, ona toxunmaq keçmişi dəyişmək olardı.
                var bildirisler = await _unitOfWork.Repository<Bildiris>()
                    .HamisiniGetirAsync(x => x.MezuniyyetId == mezuniyyetId
                                          && x.Nov == BildirisNovu.MezuniyyetMuraciet
                                          && !x.Oxunub
                                          && !x.Silinib);

                if (bildirisler.Count == 0) return Result<int>.Ok(0);

                foreach (var b in bildirisler)
                {
                    b.Silinib       = true;
                    b.SilinmeTarixi = DateTime.Now;
                    await _unitOfWork.Repository<Bildiris>().YenileAsync(b);
                }

                await _unitOfWork.YaddaSaxlaAsync();
                return Result<int>.Ok(bildirisler.Count);
            }
            catch (Exception ex)
            {
                return Result<int>.Fail($"Xəta: {ex.Message}");
            }
        }
    }
}
