using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Emeliyyat;
using FinNex.Application.Helpers.Emeliyyat;
using FinNex.Application.Interfaces.Emeliyyat;
using FinNex.Domain.Entities.Emeliyyat;
using FinNex.Domain.Entities.HR;
using FinNex.Application.Services.Hevale;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;   // AnyAsync — limit yoxlamasında sənəd növü
using GedenHevaleEntity = FinNex.Domain.Entities.Hevale.GedenHevale;

namespace FinNex.Application.Services.Emeliyyat;

// `partial` — 20 000 USD aylıq limitinin məntiqi ayrıca fayldadır:
// `KocurmeLimit.cs`. Hüquqi tələbdir, dəyişəndə harada olduğu görünsün.
public partial class KocurmeService : IKocurmeService
{
    private readonly IUnitOfWork _uow;

    // MB kursu üçün — Oracle `func_get_kurval`. Limit «ekvivalent» tələb edir.
    private readonly FinNex.Application.Interfaces.Kurval.IBmiValyutaService _valyuta;

    public KocurmeService(IUnitOfWork uow,
                          FinNex.Application.Interfaces.Kurval.IBmiValyutaService valyuta)
    {
        _uow = uow;
        _valyuta = valyuta;
    }

    // Həvalə № prefiksi (BMI: pul köçürməsi "T", tələbə köçürməsi "TL")
    private static string Prefiks(string novu) =>
        string.Equals(novu, "Telebe", StringComparison.OrdinalIgnoreCase)
            ? HevaleNomreHelper.TelebePrefiksi
            : HevaleNomreHelper.PulPrefiksi;

    // HevaleNo sonundakı rəqəm — "26-T-19" → 19
    private static int SonReqem(string? no)
    {
        if (string.IsNullOrWhiteSpace(no)) return 0;
        var son = no.Trim().Split('-').LastOrDefault();
        return int.TryParse(son, out var n) ? n : 0;
    }

    private static string TamAd(string? ad, string? soyad, string? ata)
    {
        var hisse = new[] { soyad, ad, ata }.Where(s => !string.IsNullOrWhiteSpace(s));
        var s = string.Join(" ", hisse).Trim();
        return s;
    }

    private async Task<Dictionary<int, string?>> IcraciAdMapAsync()
    {
        var isciler = await _uow.Repository<Isci>().HamisiniGetirAsync(
            predicate: x => !x.Silinib && x.IcraciNo != null, izlemeden: true);
        return isciler.Where(i => i.IcraciNo.HasValue)
            .GroupBy(i => i.IcraciNo!.Value)
            .ToDictionary(g => g.Key, g => g.First().TamAd);
    }

    public async Task<IList<KocurmeListDto>> HamisiniGetirAsync(string novu, int? il = null)
    {
        var list = await _uow.Repository<Kocurme>().HamisiniGetirAsync(
            predicate: x => !x.Silinib && x.Novu == novu
                            && (il == null || (x.Tarix != null && x.Tarix.Value.Year == il)),
            izlemeden: true);

        var adMap = await IcraciAdMapAsync();

        // Sənəd növünün ADI — bir sorğu ilə xəritə. Sətir-sətir oxusaq N+1
        // olardı; siyahıda yüzlərlə qeyd ola bilər.
        var senedMap = await _uow.Repository<KocurmeSenedNovu>().Query()
            .AsNoTracking()
            .ToDictionaryAsync(x => x.Id, x => x.Ad);

        return list
            .OrderByDescending(x => x.Tarix)
            .ThenByDescending(x => SonReqem(x.HevaleNo))
            .Select(x => new KocurmeListDto
            {
                Id            = x.Id,
                HevaleNo      = x.HevaleNo,
                Tarix         = x.Tarix,
                GonderenTamAd = TamAd(x.GonderenAd, x.GonderenSoyad, x.GonderenAtaAd),
                AlanTamAd     = TamAd(x.AlanAd, x.AlanSoyad, x.AlanAtaAd),
                Mebleg        = x.Mebleg,
                KocurulenValyuta = x.KocurulenValyuta,
                BankAd        = x.BankAd,
                Icra          = x.Icra,
                IcraciAd      = (x.Icra.HasValue && adMap.TryGetValue(x.Icra.Value, out var ad)) ? ad : null,
                YaradanId     = x.YaradanIcraciId,
                GonderenFin   = x.GonderenFin,
                UsdEkvivalent = x.UsdEkvivalent,
                SenedNovuAd   = (x.SenedNovuId.HasValue && senedMap.TryGetValue(x.SenedNovuId.Value, out var sn)) ? sn : null,
                LimitQeydi    = x.LimitQeydi
            })
            .ToList();
    }

    public async Task<string> NovbetiHevaleNoAsync(string novu)
    {
        var il = DateTime.Now.Year;
        return await HevaleNomreHelper.NovbetiAsync(_uow, il, Prefiks(novu));
    }

    public async Task<KocurmeCreateDto?> TekrarMelumatiAsync(int id, string novu)
    {
        var e = await _uow.Repository<Kocurme>().GetirAsync(
            x => x.Id == id && x.Novu == novu && !x.Silinib, izlemeden: true);
        if (e == null) return null;

        return new KocurmeCreateDto
        {
            HevaleNo         = await NovbetiHevaleNoAsync(novu),
            Tarix            = DateTime.Today,
            GonderenAd       = e.GonderenAd,
            GonderenSoyad    = e.GonderenSoyad,
            GonderenAtaAd    = e.GonderenAtaAd,
            GonderenPassport = e.GonderenPassport,
            GonderenTelefon  = e.GonderenTelefon,
            AlanAd           = e.AlanAd,
            AlanSoyad        = e.AlanSoyad,
            AlanAtaAd        = e.AlanAtaAd,
            AlanPassport     = e.AlanPassport,
            AlanTelefon      = e.AlanTelefon,
            Mebleg           = e.Mebleg,
            RialCbar         = e.RialCbar,
            ValyutaCbar      = e.ValyutaCbar,
            IranRial         = e.IranRial,
            MedaxilValyuta   = e.MedaxilValyuta,
            KocurulenValyuta = e.KocurulenValyuta,
            Secim            = e.Secim,
            BankAd           = e.BankAd,
            Filial           = e.Filial,
            AlanHesab        = e.AlanHesab,
            Elave            = e.Elave,
            Meqsed           = e.Meqsed,
            Qeyd             = e.Qeyd,

            // FİN köçürülür — təkrar adətən EYNİ göndərənə görə edilir.
            GonderenFin      = e.GonderenFin
            // ⚠️ SenedNovuId / LimitQeydi QƏSDƏN köçürülmür: onlar KEÇMİŞ
            // əməliyyatın əsaslandırmasıdır. Köçsəydi operator yeni köçürməni
            // köhnə sənədlə, yenidən baxmadan keçirə bilərdi.
        };
    }

    public async Task<Result<int>> YaratAsync(string novu, KocurmeCreateDto dto, int yaradanUserId)
    {
        if (string.IsNullOrWhiteSpace(dto.GonderenAd) && string.IsNullOrWhiteSpace(dto.AlanAd))
            return Result<int>.Fail("Ən azı göndərən və ya alan adı daxil edilməlidir.");

        var isci = (await _uow.Repository<Isci>().HamisiniGetirAsync(
            predicate: x => x.AppUserId == yaradanUserId && !x.Silinib, izlemeden: true)).FirstOrDefault();
        short? icraNo = (isci?.IcraciNo is int no && no > 0 && no <= short.MaxValue) ? (short)no : (short?)null;

        var tarix = dto.Tarix ?? DateTime.Now;
        var il = tarix.Year;

        // Nömrə YAZILAN anda yenidən hesablanır (forma preview-dan sonra başqası
        // nömrə almış ola bilər). Mənbə eyni helper-dir — bax: NovbetiHevaleNoAsync.
        var hevaleNo = await HevaleNomreHelper.NovbetiAsync(_uow, il, Prefiks(novu));

        var e = new Kocurme { Novu = novu, HevaleNo = hevaleNo, YaradanIcraciId = yaradanUserId, Icra = icraNo };
        Doldur(e, dto);

        // ⚠️ LİMİT YOXLAMASI TRANZAKSİYADAN VƏ NÖMRƏDƏN ƏVVƏLDİR.
        // `HevaleNomreHelper.NovbetiAsync` yuxarıda artıq nömrə hesablayıb,
        // amma HEÇ NƏ YAZILMAYIB — yazılan an tranzaksiyanın içidir. Yoxlama
        // burada uğursuz olsa heç bir sətir yaranmır, nömrə də «yeyilmir»
        // (CLAUDE.md — «nömrə ayrılmadan ƏVVƏL bütün yoxlamalar»).
        var limit = await LimitTetbiqEtAsync(e, novu, dto, xaricId: null);
        if (!limit.Success)
            return Result<int>.Fail(limit.Message ?? "Aylıq limit yoxlamasından keçmədi.");

        // Köçürmə + jurnal sətri BİR tranzaksiyada. Ayrı-ayrı yazılsa, ikinci yazı
        // sınanda nömrə «yeyilmiş», jurnal isə boş qalardı — nömrə geri qaytarılmır.
        using var tx = await _uow.BeginTransactionAsync();

        await _uow.Repository<Kocurme>().YaratAsync(e);
        await _uow.YaddaSaxlaAsync();   // e.Id burada yaranır — jurnal bağı üçün lazımdır

        if (HevaleJurnalinaDuser(novu))
        {
            var h = new GedenHevaleEntity
            {
                HevNom          = hevaleNo,
                KocurmeId       = e.Id,
                Icra            = icraNo,
                YaradanIcraciId = yaradanUserId
            };
            HevaleSetriniDoldur(h, e);

            await _uow.Repository<GedenHevaleEntity>().YaratAsync(h);
            await _uow.YaddaSaxlaAsync();
        }

        await tx.CommitAsync();

        return Result<int>.Ok(e.Id, $"Köçürmə qeydə alındı — № {hevaleNo}.");
    }

    // ══════════════════════════════════════════════════════════════════════
    // GEDƏN HƏVALƏ BAĞI (18.08.2026)
    //
    // İstifadəçi qaydası: «həvalə nömrəsi Gedən həvaləyə yazılır, ƏSAS budur,
    // nömrə ordan gəlir; eyni qaydada həmin nömrə köçürmələrə qeyd edilir».
    // Yəni GedenHevale ƏSAS jurnaldır — Pul köçürməsi ora sətir yazır və eyni
    // nömrəni özündə də saxlayır. Nömrənin özü onsuz da hər iki cədvələ baxan
    // `HevaleNomreHelper`-dən gəlir.
    //
    // YALNIZ «Pul» köçürməsi: Tələbə köçürməsi «TL» prefiksi ilə AYRI nömrə
    // fəzasındadır və Gedən həvalə jurnalı (BMI odb.geden_hevale) yalnız «-T-»
    // sətirlərindən ibarətdir.
    // ══════════════════════════════════════════════════════════════════════
    // Şərt PREFİKSƏ bağlanıb, növ adına yox: jurnal «-T-» fəzasıdır. Sabah yeni bir
    // Novu əlavə olunsa, «T» nömrəsi alan hər köçürmə avtomatik jurnala da düşür —
    // iki qayda ayrı-ayrı yazılsa biri köhnə qalardı.
    private static bool HevaleJurnalinaDuser(string novu) =>
        Prefiks(novu) == HevaleNomreHelper.PulPrefiksi;

    // BMI sütun uzunluqları Kocurme-dəkindən DARDIR (SAA 50 ↔ ad sahələri 3×80,
    // AL_BANK 40 ↔ BankAd 120). Kəsmədən yazsaq SQL «String or binary data would
    // be truncated» ilə bütün əməliyyatı sındırardı.
    private static string? Kes(string? s, int max)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var t = s.Trim();
        return t.Length <= max ? t : t[..max];
    }

    /// <summary>
    /// Köçürmədən Gedən həvalə sətrinə sahə köçürməsi.
    ///
    /// TƏSDİQLƏNMİŞ UYĞUNLUQ (7 sahə):
    ///   HEV_NOM ← HevaleNo · TARIX ← Tarix · SAA ← göndərənin Soyad Ad Ata
    ///   MEBLEG  ← KÖÇÜRÜLƏN məbləğ (Rial/Rubl-da Mebleg × IranRial — bax
    ///             <see cref="KocurmeValyuta"/>; ALINAN məbləğ DEYİL)
    ///   VAL_TIP ← köçürülən valyuta KODU · AL_BANK ← BankAd · ICRA ← Icra
    ///
    /// VAL_TIP-ə tam ad («ABŞ dolları») YAZILMIR: sütun nvarchar(10)-dur, tam ad
    /// 11 simvoldur və səssizcə «ABŞ dollar» kimi kəsilərdi. Kod («USD», «Rial»,
    /// «Avro», «Rubl», «AZN») onsuz da 10-a sığır və sütunun adına («valyuta tipi»)
    /// uyğundur.
    ///
    /// QƏSDƏN BOŞ BURAXILAN (qaydası hələ təsdiqlənməyib — uydurma dəyər yazmaqdansa
    /// boş qalması yaxşıdır; jurnal sətri əl ilə tamamlana bilər):
    ///   HES_NOM (Hesab № — göndərənin, yoxsa alanın?), OLKE (təyinat ölkə —
    ///   Pul köçürməsi formasında ölkə sahəsi YOXDUR), TIP_RES (rezident tipi),
    ///   HEV_TIP, GON_TIP, MEN_OLKE, CONTRAC_NOM, DECLAR_NOM, ARAYIS.
    /// Qayda dəqiqləşəndə YALNIZ bu metod dəyişməlidir — yaratma və redaktə
    /// yollarının hər ikisi onu çağırır.
    ///
    /// MEBLEG sütunu decimal(14,2)-dir (Kocurme-də 18,2). Rial çevrilməsində məbləğ
    /// böyüyür — 12 rəqəmdən artıq tam hissə SQL overflow verər. Real köçürmələrdə
    /// (900 × 850 000 = 765 000 000) uzaq həddir; klamp QOYULMUR ki, belə hal səssizcə
    /// yanlış məbləğ yazmasın, açıq xəta versin.
    /// </summary>
    private static void HevaleSetriniDoldur(GedenHevaleEntity h, Kocurme k)
    {
        h.Tarix  = k.Tarix;
        h.Saa    = Kes(TamAd(k.GonderenAd, k.GonderenSoyad, k.GonderenAtaAd), 50);
        h.Mebleg = KocurmeValyuta.KocurulenMebleg(k.KocurulenValyuta, k.Mebleg, k.IranRial);
        h.ValTip = Kes(k.KocurulenValyuta, 10);
        h.AlBank = Kes(k.BankAd, 40);
    }

    // Köçürməyə bağlı jurnal sətri (varsa). Bağ AÇIQ sahə ilədir (KocurmeId),
    // nömrə ilə DEYİL — mövcud datada nömrə hələ unikal deyil (test «26-T-1»
    // real BMI idxalı «26-T-1» ilə üst-üstə düşür), nömrə ilə axtarsaq köçürmənin
    // silinməsi REAL jurnal sətrini silərdi.
    private Task<GedenHevaleEntity?> BagliHevaleAsync(int kocurmeId) =>
        _uow.Repository<GedenHevaleEntity>().GetirAsync(x => x.KocurmeId == kocurmeId && !x.Silinib);

    // Form dəyərlərini voucher açarlarına çevir
    private static string SecimAcar(string? s) => s switch
    {
        "Hesab və mədaxil" => "vemedaxil",
        "Hesabdan" => "hesabdan",
        _ => "acmadan"
    };

    // Sahələrdən voucher Input qurur (BMI açar map)
    private FinNex.Application.Helpers.Emeliyyat.PulKocurmeVoucher.Input BuildInput(
        string? secim, string? kocurulen, string? medaxil, decimal? mebleg, decimal? iranRial,
        decimal? rialCbar, decimal? valyutaCbar, string? musteriHesabi, string? bankAd, string? filial,
        string? hevale, string? meqsed, string? gAd, string? gSoyad, string? gAta,
        string? aAd, string? aSoyad, string? aAta)
        => new()
        {
            Secim       = SecimAcar(secim),
            Kocurulen   = string.IsNullOrWhiteSpace(kocurulen) ? "USD" : kocurulen!,
            Medaxil     = string.IsNullOrWhiteSpace(medaxil) ? "USD" : medaxil!,
            Mebleg      = mebleg ?? 0m,
            IranRial    = iranRial ?? 0m,
            RialCbar    = rialCbar ?? 0m,
            ValyutaCbar = valyutaCbar ?? 0m,
            MusteriHesabi = musteriHesabi,
            BankAdi     = bankAd,
            Filial      = filial,
            Hevale      = hevale,
            Meqsed      = meqsed,
            AlanAdi     = TamAd(aAd, aSoyad, aAta),
            GonderenTamAd = $"({TamAd(gAd, gSoyad, gAta)})"
        };

    public IList<MuhasibatSetirDto> VoucherHesabla(KocurmeFormDto dto, string? hevaleNo)
    {
        var input = BuildInput(dto.Secim, dto.KocurulenValyuta, dto.MedaxilValyuta, dto.Mebleg,
            dto.IranRial, dto.RialCbar, dto.ValyutaCbar, dto.AlanHesab, dto.BankAd, dto.Filial,
            hevaleNo, dto.Meqsed, dto.GonderenAd, dto.GonderenSoyad, dto.GonderenAtaAd,
            dto.AlanAd, dto.AlanSoyad, dto.AlanAtaAd);
        return FinNex.Application.Helpers.Emeliyyat.PulKocurmeVoucher.Qur(input);
    }

    public async Task<KocurmeDetalDto?> DetalAsync(int id, string novu)
    {
        var e = await _uow.Repository<Kocurme>().GetirAsync(
            x => x.Id == id && x.Novu == novu && !x.Silinib, izlemeden: true);
        if (e == null) return null;

        var input = BuildInput(e.Secim, e.KocurulenValyuta, e.MedaxilValyuta, e.Mebleg, e.IranRial,
            e.RialCbar, e.ValyutaCbar, e.AlanHesab, e.BankAd, e.Filial, e.HevaleNo, e.Meqsed,
            e.GonderenAd, e.GonderenSoyad, e.GonderenAtaAd, e.AlanAd, e.AlanSoyad, e.AlanAtaAd);

        return new KocurmeDetalDto
        {
            Id            = e.Id,
            HevaleNo      = e.HevaleNo,
            Tarix         = e.Tarix,
            GonderenTamAd = TamAd(e.GonderenAd, e.GonderenSoyad, e.GonderenAtaAd),
            AlanTamAd     = TamAd(e.AlanAd, e.AlanSoyad, e.AlanAtaAd),
            GonderenAd    = e.GonderenAd,
            GonderenSoyad = e.GonderenSoyad,
            GonderenAtaAd = e.GonderenAtaAd,
            GonderenTelefon = e.GonderenTelefon,
            AlanAd        = e.AlanAd,
            AlanSoyad     = e.AlanSoyad,
            AlanAtaAd     = e.AlanAtaAd,
            GonderenPassport = e.GonderenPassport,
            AlanPassport  = e.AlanPassport,
            Elave         = e.Elave,
            Qeyd          = e.Qeyd,
            Mebleg        = e.Mebleg,
            MedaxilValyuta   = e.MedaxilValyuta,
            KocurulenValyuta = e.KocurulenValyuta,
            Secim         = e.Secim,
            IranRial      = e.IranRial,
            RialCbar      = e.RialCbar,
            ValyutaCbar   = e.ValyutaCbar,
            BankAd        = e.BankAd,
            Filial        = e.Filial,
            AlanHesab     = e.AlanHesab,
            Meqsed        = e.Meqsed,
            YaradanId     = e.YaradanIcraciId,
            Setirler      = FinNex.Application.Helpers.Emeliyyat.PulKocurmeVoucher.Qur(input)
        };
    }

    public async Task<KocurmeEditDto?> RedakteMelumatiAsync(int id, string novu)
    {
        var e = await _uow.Repository<Kocurme>().GetirAsync(
            x => x.Id == id && x.Novu == novu && !x.Silinib, izlemeden: true);
        if (e == null) return null;

        return new KocurmeEditDto
        {
            Id               = e.Id,
            HevaleNo         = e.HevaleNo,
            YaradanId        = e.YaradanIcraciId,
            Tarix            = e.Tarix,
            GonderenAd       = e.GonderenAd,
            GonderenSoyad    = e.GonderenSoyad,
            GonderenAtaAd    = e.GonderenAtaAd,
            GonderenPassport = e.GonderenPassport,
            GonderenTelefon  = e.GonderenTelefon,
            AlanAd           = e.AlanAd,
            AlanSoyad        = e.AlanSoyad,
            AlanAtaAd        = e.AlanAtaAd,
            AlanPassport     = e.AlanPassport,
            AlanTelefon      = e.AlanTelefon,
            Mebleg           = e.Mebleg,
            RialCbar         = e.RialCbar,
            ValyutaCbar      = e.ValyutaCbar,
            IranRial         = e.IranRial,
            MedaxilValyuta   = e.MedaxilValyuta,
            KocurulenValyuta = e.KocurulenValyuta,
            Secim            = e.Secim,
            BankAd           = e.BankAd,
            Filial           = e.Filial,
            AlanHesab        = e.AlanHesab,
            Elave            = e.Elave,
            Meqsed           = e.Meqsed,
            Qeyd             = e.Qeyd,
            GonderenFin      = e.GonderenFin,
            SenedNovuId      = e.SenedNovuId,
            LimitQeydi       = e.LimitQeydi
        };
    }

    public async Task<Result> YenileAsync(string novu, KocurmeEditDto dto, int userId, bool isAdmin)
    {
        var e = await _uow.Repository<Kocurme>().GetirAsync(x => x.Id == dto.Id && x.Novu == novu && !x.Silinib);
        if (e == null) return Result.Fail("Köçürmə tapılmadı.");
        if (!isAdmin && e.YaradanIcraciId != userId)
            return Result.Fail("Yalnız öz qeydinizi və ya Admin dəyişə bilər.");

        Doldur(e, dto);   // HevaleNo dəyişməz

        // ⚠️ REDAKTƏDƏ `xaricId` MƏCBURİDİR — qeydin ÖZÜ cəmdən çıxarılmalıdır.
        // Olmasa 10 000-lik köçürməni açan operator cəmdə həmin 10 000-i də
        // görər və məbləği artırmadan «limit aşıldı» xəbərdarlığı alar
        // (məzuniyyət tarix konfliktində eyni qayda).
        var limit = await LimitTetbiqEtAsync(e, novu, dto, xaricId: e.Id);
        if (!limit.Success) return limit;

        e.YenileyenIcraciId = userId;
        e.YenilenmeTarixi   = DateTime.Now;

        await _uow.Repository<Kocurme>().YenileAsync(e);

        // Jurnal sətri köçürmənin güzgüsüdür — köçürmə dəyişəndə o da dəyişməlidir,
        // yoxsa Gedən həvalə səhifəsində köhnə məbləğ/ad qalar və heç bir xəta çıxmaz.
        // Nömrə (HEV_NOM) burada da toxunulmazdır.
        var h = await BagliHevaleAsync(e.Id);
        if (h != null)
        {
            HevaleSetriniDoldur(h, e);
            h.YenileyenIcraciId = userId;
            h.YenilenmeTarixi   = DateTime.Now;
            await _uow.Repository<GedenHevaleEntity>().YenileAsync(h);
        }

        await _uow.YaddaSaxlaAsync();
        return Result.Ok("Köçürmə yeniləndi.");
    }

    public async Task<Result> SilAsync(int id, int userId, bool isAdmin)
    {
        var e = await _uow.Repository<Kocurme>().GetirAsync(x => x.Id == id && !x.Silinib);
        if (e == null) return Result.Fail("Köçürmə tapılmadı.");
        if (!isAdmin && e.YaradanIcraciId != userId)
            return Result.Fail("Yalnız öz qeydinizi və ya Admin silə bilər.");

        e.Silinib       = true;
        e.SilinmeTarixi = DateTime.Now;
        e.SilenIcraciId = userId;

        await _uow.Repository<Kocurme>().YenileAsync(e);

        // Köçürmə silinirsə jurnal sətri də silinir (yumşaq) — əks halda Gedən
        // həvalədə sahibsiz sətir qalardı. NÖMRƏ GERİ QAYTARILMIR: `HevaleNomreHelper`
        // `QueryAll()` ilə silinmişləri də sayır, ona görə həmin nömrə təkrar verilmir.
        var h = await BagliHevaleAsync(e.Id);
        if (h != null)
        {
            h.Silinib       = true;
            h.SilinmeTarixi = DateTime.Now;
            h.SilenIcraciId = userId;
            await _uow.Repository<GedenHevaleEntity>().YenileAsync(h);
        }

        await _uow.YaddaSaxlaAsync();
        return Result.Ok("Köçürmə silindi.");
    }

    // Ortaq sahələri entity-yə köçürür (HevaleNo/Novu/Icra toxunulmur)
    private static void Doldur(Kocurme e, KocurmeFormDto dto)
    {
        e.Tarix            = dto.Tarix;
        e.GonderenAd       = dto.GonderenAd?.Trim();
        e.GonderenSoyad    = dto.GonderenSoyad?.Trim();
        e.GonderenAtaAd    = dto.GonderenAtaAd?.Trim();
        e.GonderenPassport = dto.GonderenPassport?.Trim();
        e.GonderenTelefon  = dto.GonderenTelefon?.Trim();
        e.AlanAd           = dto.AlanAd?.Trim();
        e.AlanSoyad        = dto.AlanSoyad?.Trim();
        e.AlanAtaAd        = dto.AlanAtaAd?.Trim();
        e.AlanPassport     = dto.AlanPassport?.Trim();
        e.AlanTelefon      = dto.AlanTelefon?.Trim();
        e.Mebleg           = dto.Mebleg;
        e.RialCbar         = dto.RialCbar;
        e.ValyutaCbar      = dto.ValyutaCbar;
        e.IranRial         = dto.IranRial;
        e.MedaxilValyuta   = dto.MedaxilValyuta?.Trim();
        e.KocurulenValyuta = dto.KocurulenValyuta?.Trim();
        e.Secim            = dto.Secim?.Trim();
        e.BankAd           = dto.BankAd?.Trim();
        e.Filial           = dto.Filial?.Trim();
        e.AlanHesab        = dto.AlanHesab?.Trim();
        e.Elave            = dto.Elave?.Trim();
        e.Meqsed           = dto.Meqsed?.Trim();
        e.Qeyd             = dto.Qeyd?.Trim();

        // ── 20 000 USD limiti ─────────────────────────────────────────────
        // FİN NORMALLAŞDIRILIR (boşluqsuz, BÖYÜK hərf) — axtarış da eyni
        // metoddan keçir; biri normallaşdırıb o biri normallaşdırmasa
        // «5ab2cd1» və «5AB2CD1» iki ayrı şəxs sayılar və limit ikiqat açılar.
        var fin = FinTemizle(dto.GonderenFin);
        e.GonderenFin = fin.Length > 0 ? fin : null;

        // SenedNovuId / LimitQeydi BURADA YAZILMIR — onları çağıran metod
        // (YaratAsync / YenileAsync) YOXLAMADAN SONRA yazır. Burada yazsaq,
        // limit aşılmadığı halda da köhnə sənəd bağlı qalardı.
    }

    /// <summary>
    /// Limit yoxlaması + USD ekvivalentinin YAZILMASI — yaratma və redaktə
    /// yollarının ORTAQ addımı.
    ///
    /// ⚠️ Yazma yolu İKİDİR (`YaratAsync`, `YenileAsync`). Yoxlamanı hər
    /// ikisində ayrıca yazsaq biri gec-tez köhnə qalar və limit yalnız o
    /// yolda yan keçilər (CLAUDE.md — «yoxlama BÜTÜN giriş nöqtələrində»).
    /// Ona görə tək metoddur.
    /// </summary>
    private async Task<Result> LimitTetbiqEtAsync(
        Kocurme e, string novu, KocurmeFormDto dto, int? xaricId)
    {
        var tarix = dto.Tarix ?? DateTime.Now;

        // Limitə düşməyən növ (Tələbə) — USD ekvivalenti də yazılmır,
        // sənəd sahələri təmizlənir.
        if (!string.Equals(novu, "Pul", StringComparison.OrdinalIgnoreCase))
        {
            e.UsdEkvivalent = null;
            e.UsdKursu      = null;
            e.SenedNovuId   = null;
            e.LimitQeydi    = null;
            return Result.Ok();
        }

        var yox = await LimitYoxlaAsync(novu, e.GonderenFin, dto.Mebleg,
                                        dto.MedaxilValyuta, tarix, xaricId);

        // Oracle kursu alınmadı → BLOK (istifadəçi qərarı 07.09.2026).
        // Kurssuz yazsaq aylıq cəm səssizcə əskik qalardı və limit
        // növbəti əməliyyatlarda yanlış hesablanardı.
        // ⚠️ `MesajDuz` MƏCBURİDİR — mətndəki `**` yalnız ekran vurğusudur.
        //    Buradan çıxan mesaj `TempData`-ya düşür, Razor onu HTML-kodlaşdırır
        //    və nişan atılmasa istifadəçi hərfi `**19.117,65 USD**` görər.
        if (yox.KursAlinmadi)
            return Result.Fail(MesajDuz(yox.Mesaj) is { Length: > 0 } m1
                ? m1 : "Valyuta kursu alınmadı — əməliyyat qeydə alınmadı.");

        if (yox.SenedTelebOlunur && (dto.SenedNovuId is null or <= 0))
            return Result.Fail(
                (MesajDuz(yox.Mesaj) is { Length: > 0 } m2 ? m2 : "Aylıq limit aşılır.") +
                " Əsas sənədin növü seçilmədən qeyd yadda saxlanıla bilməz.");

        // Seçilmiş sənəd növü HƏQİQƏTƏN mövcud və aktiv olmalıdır — forma
        // dəyəri uydurula bilər (hazırkı POST-a inanmırıq).
        if (dto.SenedNovuId is int sid && sid > 0)
        {
            var novVar = await _uow.Repository<KocurmeSenedNovu>().Query()
                .AnyAsync(x => x.Id == sid && !x.Silinib && x.Aktivdir);
            if (!novVar)
                return Result.Fail("Seçilmiş sənəd növü tapılmadı və ya deaktivdir.");
        }

        // USD ekvivalenti YAZILAN anda dondurulur — sonrakı kurs dəyişikliyi
        // keçmiş ayın cəmini tərpətməsin.
        var (usd, kurs) = await UsdEkvivalentAsync(dto.Mebleg, dto.MedaxilValyuta, tarix);
        e.UsdEkvivalent = usd;
        e.UsdKursu      = kurs;

        // Sənəd yalnız limit aşılanda saxlanılır — aşılmayıbsa təmizlənir
        // (redaktədə məbləğ azaldılsa köhnə sənəd asılı qalmasın).
        e.SenedNovuId = yox.SenedTelebOlunur ? dto.SenedNovuId : null;
        e.LimitQeydi  = yox.SenedTelebOlunur ? dto.LimitQeydi?.Trim() : null;

        return Result.Ok();
    }
}
