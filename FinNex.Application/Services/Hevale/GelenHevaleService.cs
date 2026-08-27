using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Hevale;
using FinNex.Application.Interfaces.Hevale;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Entities.Hevale;
using FinNex.Domain.Interfaces;

namespace FinNex.Application.Services.Hevale;

public class GelenHevaleService : IGelenHevaleService
{
    private readonly IUnitOfWork _uow;

    public GelenHevaleService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    // YALNIZ SIRALAMA üçün — nömrə VERMİR (nömrə əl ilə yazılır, bax: NomreYoxlaAsync).
    // Formatdan asılı olmayan sadə oxunuş: "046001" → 46001, "22-G-82" → 82.
    // Eyni tarixli sətirləri təxmini nömrə sırası ilə düzür, başqa təsiri yoxdur.
    private static int SonReqem(string? hevNom)
    {
        if (string.IsNullOrWhiteSpace(hevNom)) return 0;
        var son = hevNom.Trim().Split('-').LastOrDefault();
        return int.TryParse(son, out var n) ? n : 0;
    }

    // ── HƏVALƏ № — 2026-da ƏL İLƏ, 2027-dən avtomatik ─────────────────────
    //
    // BMI-də bu jurnalın nömrəsi `{VV}{Y}{NNN}` formasındadır — 04(valyuta) +
    // 6(ilin son rəqəmi) + 001(sıra). 13.08.2026 yoxlaması: 10 596 sətrin
    // hamısı bu formadadır (cəmi 3 istisna: iki "22-G-8#" və bir "150062.").
    //
    // Əvvəl kod `{YY}-{N}` avtomatik yazırdı — bu, BMI qaydası ilə ümumiyyətlə
    // uyğun deyildi və nömrələmə səhv gedirdi. İndi istifadəçi ÖZÜ yazır
    // (bu günə qədər kağız jurnaldan götürdüyü kimi).
    //
    // 2027 planı: `{VV}{YY}{NNN}` — valyuta kodu kurval (SOKNAMEVALUT)
    // siyahısından, sıra (valyuta, il) üzrə sayğacdan. O vaxt bu metod
    // avtomatik nömrə verəcək; indi yalnız dublikatı yoxlayır.
    //
    // YOXLAMA BÜTÜN İLLƏR ÜZRƏDİR — nömrənin içində il var (`26` hissəsi),
    // ona görə eyni nömrə heç bir ildə təkrarlanmamalıdır.
    // Gələn həvalə nömrəsi ƏL İLƏ yazılır (jurnaldan) — sistem nömrə vermir.
    // Ona görə burada SİLİNMİŞLƏR QƏSDƏN sayılmır (`HamisiniGetirAsync` avtomatik
    // `!Silinib` tətbiq edir): səhv nömrə yazılıb qeyd silinibsə, düzgün nömrənin
    // yenidən yazılmasına mane olmamalıdır. Bu, avtomatik nömrələnən jurnallardan
    // (XaricMektub / DaxilMektub / GedenHevale — orada QueryAll işlədilir, çünki
    // sənəd artıq o nömrə ilə gedib) QƏSDƏN fərqlidir. 13.08.2026 qərarı.
    private async Task<Result> NomreYoxlaAsync(string? hevNom, int? istisnaId = null)
    {
        if (string.IsNullOrWhiteSpace(hevNom))
            return Result.Fail("Həvalə № boş ola bilməz — jurnaldakı nömrəni yazın.");

        var nom = hevNom.Trim();

        var movcuddur = (await _uow.Repository<GelenHevale>().HamisiniGetirAsync(
                x => !x.Silinib && x.HevNom != null, izlemeden: true))
            .Any(x => string.Equals(x.HevNom!.Trim(), nom, StringComparison.OrdinalIgnoreCase)
                   && (istisnaId == null || x.Id != istisnaId.Value));

        return movcuddur
            ? Result.Fail($"Həvalə № «{nom}» artıq mövcuddur — jurnal nömrəsi təkrarlana bilməz.")
            : Result.Ok();
    }

    private async Task<Dictionary<int, string?>> IcraciAdMapAsync()
    {
        var isciler = await _uow.Repository<Isci>().HamisiniGetirAsync(
            predicate: x => !x.Silinib && x.IcraciNo != null, izlemeden: true);
        return isciler.Where(i => i.IcraciNo.HasValue)
            .GroupBy(i => i.IcraciNo!.Value)
            .ToDictionary(g => g.Key, g => g.First().TamAd);
    }

    // Jurnalda mövcud illər və icraçı nömrələri (filtr açılan siyahıları üçün).
    // Həvalədə `Il` sütunu yoxdur — il `Tarix`-dən çıxarılır.
    public async Task<HevaleFiltrMenbeDto> FiltrMenbeleriAsync()
    {
        var hamisi = await _uow.Repository<GelenHevale>()
            .HamisiniGetirAsync(x => !x.Silinib, izlemeden: true);

        var adMap = await IcraciAdMapAsync();

        return new HevaleFiltrMenbeDto
        {
            Iller = hamisi.Where(x => x.Tarix.HasValue).Select(x => x.Tarix!.Value.Year)
                          .Distinct().OrderByDescending(x => x).ToList(),

            Icracilar = hamisi
                .Where(x => x.Icra.HasValue)
                .GroupBy(x => x.Icra!.Value)
                .Select(g => new HevaleIcraciDto
                {
                    No  = g.Key,
                    Ad  = adMap.TryGetValue(g.Key, out var ad) ? ad : null,
                    Say = g.Count()
                })
                .OrderBy(x => x.No)
                .ToList()
        };
    }

    public async Task<HevaleSehifeDto<GelenHevaleListDto>> HamisiniGetirAsync(HevaleFiltrDto? filtr = null)
    {
        var f  = HevaleFiltrDto.Normalla(filtr);
        var il = f.SorguIli;   // "bütün illər" seçilibsə null

        // İl, icraçı və tarix DB tərəfində süzülür; yalnız mətn axtarışı yaddaşdadır.
        var list = await _uow.Repository<GelenHevale>().HamisiniGetirAsync(
            predicate: x => !x.Silinib
                         && (il == null || (x.Tarix != null && x.Tarix.Value.Year == il))
                         && (f.IcraciNo == null || x.Icra == f.IcraciNo)
                         && (f.TarixFrom == null || (x.Tarix != null && x.Tarix >= f.TarixFrom))
                         && (f.TarixTo   == null || (x.Tarix != null && x.Tarix <= f.TarixTo)),
            izlemeden: true);

        if (!string.IsNullOrWhiteSpace(f.Axtaris))
        {
            var q = f.Axtaris.Trim();
            list = list.Where(x =>
                    (x.Saa     ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (x.HevNom  ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (x.HesNom  ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (x.AlBank  ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (x.GelOlke ?? "").Contains(q, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        var adMap = await IcraciAdMapAsync();

        // Səhifələmə süzgəclərdən SONRA — "Cəmi" filtrə uyğun sətir sayını göstərməlidir.
        return new HevaleSehifeDto<GelenHevaleListDto>
        {
            CemiSay      = list.Count,
            Sehife       = f.Sehife,
            SehifeOlcusu = f.SehifeOlcusu,
            Setirler     = list
            .OrderByDescending(x => x.Tarix)
            .ThenByDescending(x => SonReqem(x.HevNom))
            .Skip((f.Sehife - 1) * f.SehifeOlcusu)
            .Take(f.SehifeOlcusu)
            .Select(x => new GelenHevaleListDto
            {
                Id        = x.Id,
                HevNom    = x.HevNom,
                Tarix     = x.Tarix,
                Saa       = x.Saa,
                Mebleg    = x.Mebleg,
                ValTip    = x.ValTip,
                GelOlke   = x.GelOlke,
                AlBank    = x.AlBank,
                Icra      = x.Icra,
                IcraciAd  = (x.Icra.HasValue && adMap.TryGetValue(x.Icra.Value, out var ad)) ? ad : null,
                YaradanId = x.YaradanIcraciId,
                FaylYolu  = x.FaylYolu,
                FaylVar   = !string.IsNullOrEmpty(x.FaylYolu)
            })
            .ToList()
        };
    }

    // GelenHevale sütunları BMI ölçülərindədir (AppDbContext HasMaxLength).
    // Limitdən uzun dəyər SQL-də «String or binary data would be truncated» ilə
    // bütün yazını sındırır və istifadəçi yalnız ümumi xəta səhifəsi görür —
    // GedenHevaleService.UzunluqXetasi ilə eyni qayda (real hadisə 27.08.2026, VM).
    // Limitlər AppDbContext ilə EYNİ olmalıdır; view-lardakı maxlength birinci qatdır.
    private static string? UzunluqXetasi(GelenHevaleCreateDto d) => UzunluqXetasi(
        d.HevNom, d.Saa, d.HesNom, d.TipRes, d.ValTip, d.MenOlke,
        d.GelOlke, d.AlBank, d.HevTip, d.GonTip, d.DecNom);

    private static string? UzunluqXetasi(GelenHevaleEditDto d) => UzunluqXetasi(
        d.HevNom, d.Saa, d.HesNom, d.TipRes, d.ValTip, d.MenOlke,
        d.GelOlke, d.AlBank, d.HevTip, d.GonTip, d.DecNom);

    private static string? UzunluqXetasi(
        string? hevNom, string? saa, string? hesNom, string? tipRes, string? valTip,
        string? menOlke, string? gelOlke, string? alBank, string? hevTip,
        string? gonTip, string? decNom)
    {
        var limitler = new (string? Deyer, int Limit, string Ad)[]
        {
            (hevNom,     10, "Həvalə №"),
            (saa,        50, "Soyad Ad Ata (S.A.A.)"),
            (hesNom,     20, "Hesab №"),
            (tipRes,     16, "Rezident tipi"),
            (valTip,     10, "Valyuta"),
            (menOlke,    40, "Mənşə ölkə"),
            (gelOlke,    40, "Gəldiyi ölkə"),
            (alBank,    250, "Alan bank"),
            (hevTip,    254, "Həvalə tipi"),
            (gonTip,     20, "Göndərən tipi"),
            (decNom,     30, "Bəyannamə №"),
        };
        foreach (var (deyer, limit, ad) in limitler)
        {
            var uzunluq = deyer?.Trim().Length ?? 0;
            if (uzunluq > limit)
                return $"«{ad}» maksimum {limit} simvol ola bilər (daxil edilən: {uzunluq}). Qeyd yazılmadı.";
        }
        return null;
    }

    public async Task<Result<string>> YaratAsync(GelenHevaleCreateDto dto, int yaradanUserId, string? faylYolu = null)
    {
        if (string.IsNullOrWhiteSpace(dto.Saa))
            return Result<string>.Fail("Soyad Ad Ata (S.A.A.) boş ola bilməz.");

        if (UzunluqXetasi(dto) is string uXeta)
            return Result<string>.Fail(uXeta);

        var isci = (await _uow.Repository<Isci>().HamisiniGetirAsync(
            predicate: x => x.AppUserId == yaradanUserId && !x.Silinib, izlemeden: true)).FirstOrDefault();
        short? icraNo = (isci?.IcraciNo is int no && no > 0 && no <= short.MaxValue) ? (short)no : (short?)null;

        // Həvalə № istifadəçidən gəlir (bax: NomreYoxlaAsync şərhi)
        var nomreYoxlama = await NomreYoxlaAsync(dto.HevNom);
        if (!nomreYoxlama.Success)
            return Result<string>.Fail(nomreYoxlama.Message ?? "Həvalə № yanlışdır.");

        var hevNom = dto.HevNom!.Trim();

        var entity = new GelenHevale
        {
            HevNom   = hevNom,
            Tarix    = dto.Tarix,
            Saa      = dto.Saa?.Trim(),
            HesNom   = dto.HesNom?.Trim(),
            TipRes   = dto.TipRes?.Trim(),
            Mebleg   = dto.Mebleg,
            ValTip   = dto.ValTip?.Trim(),
            MenOlke  = dto.MenOlke?.Trim(),
            GelOlke  = dto.GelOlke?.Trim(),
            AlBank   = dto.AlBank?.Trim(),
            HevTip   = dto.HevTip?.Trim(),
            GonTip   = dto.GonTip?.Trim(),
            DecNom   = dto.DecNom?.Trim(),
            Icra     = icraNo,
            FaylYolu = string.IsNullOrWhiteSpace(faylYolu) ? null : faylYolu,
            YaradanIcraciId = yaradanUserId
        };

        await _uow.Repository<GelenHevale>().YaratAsync(entity);
        await _uow.YaddaSaxlaAsync();

        return Result<string>.Ok(hevNom, $"Gələn həvalə qeydə alındı — № {hevNom}.");
    }

    public async Task<GelenHevaleEditDto?> RedakteMelumatiAsync(int id)
    {
        var e = await _uow.Repository<GelenHevale>().GetirAsync(
            x => x.Id == id && !x.Silinib, izlemeden: true);
        if (e == null) return null;

        return new GelenHevaleEditDto
        {
            Id             = e.Id,
            Tarix          = e.Tarix,
            Saa            = e.Saa,
            HesNom         = e.HesNom,
            TipRes         = e.TipRes,
            Mebleg         = e.Mebleg,
            ValTip         = e.ValTip,
            MenOlke        = e.MenOlke,
            GelOlke        = e.GelOlke,
            AlBank         = e.AlBank,
            HevTip         = e.HevTip,
            GonTip         = e.GonTip,
            DecNom         = e.DecNom,
            HevNom         = e.HevNom,
            MovcudFaylYolu = e.FaylYolu,
            YaradanId      = e.YaradanIcraciId
        };
    }

    public async Task<Result> YenileAsync(GelenHevaleEditDto dto, int userId, bool isAdmin, string? yeniFaylYolu = null)
    {
        if (string.IsNullOrWhiteSpace(dto.Saa))
            return Result.Fail("Soyad Ad Ata (S.A.A.) boş ola bilməz.");

        if (UzunluqXetasi(dto) is string uXeta)
            return Result.Fail(uXeta);

        var e = await _uow.Repository<GelenHevale>().GetirAsync(x => x.Id == dto.Id && !x.Silinib);
        if (e == null) return Result.Fail("Həvalə tapılmadı.");
        if (!isAdmin && e.YaradanIcraciId != userId)
            return Result.Fail("Yalnız öz qeydinizi və ya Admin dəyişə bilər.");

        // Nömrə əl ilə yazıldığı üçün səhv ola bilər — düzəldilməsinə icazə verilir,
        // amma dublikat yaratmamalıdır (öz sətri yoxlamadan kənarda saxlanılır).
        var nomreYoxlama = await NomreYoxlaAsync(dto.HevNom, dto.Id);
        if (!nomreYoxlama.Success) return nomreYoxlama;

        e.HevNom  = dto.HevNom!.Trim();
        e.Tarix   = dto.Tarix;
        e.Saa     = dto.Saa?.Trim();
        e.HesNom  = dto.HesNom?.Trim();
        e.TipRes  = dto.TipRes?.Trim();
        e.Mebleg  = dto.Mebleg;
        e.ValTip  = dto.ValTip?.Trim();
        e.MenOlke = dto.MenOlke?.Trim();
        e.GelOlke = dto.GelOlke?.Trim();
        e.AlBank  = dto.AlBank?.Trim();
        e.HevTip  = dto.HevTip?.Trim();
        e.GonTip  = dto.GonTip?.Trim();
        e.DecNom  = dto.DecNom?.Trim();
        if (!string.IsNullOrWhiteSpace(yeniFaylYolu))
            e.FaylYolu = yeniFaylYolu;
        e.YenileyenIcraciId = userId;
        e.YenilenmeTarixi   = DateTime.Now;

        await _uow.Repository<GelenHevale>().YenileAsync(e);
        await _uow.YaddaSaxlaAsync();
        return Result.Ok("Həvalə yeniləndi.");
    }

    public async Task<Result> SilAsync(int id, int userId, bool isAdmin)
    {
        var e = await _uow.Repository<GelenHevale>().GetirAsync(x => x.Id == id && !x.Silinib);
        if (e == null) return Result.Fail("Həvalə tapılmadı.");
        if (!isAdmin && e.YaradanIcraciId != userId)
            return Result.Fail("Yalnız öz qeydinizi və ya Admin silə bilər.");

        e.Silinib       = true;
        e.SilinmeTarixi = DateTime.Now;
        e.SilenIcraciId = userId;

        await _uow.Repository<GelenHevale>().YenileAsync(e);
        await _uow.YaddaSaxlaAsync();
        return Result.Ok("Həvalə silindi.");
    }
}
