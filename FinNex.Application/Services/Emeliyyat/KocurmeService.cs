using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Emeliyyat;
using FinNex.Application.Interfaces.Emeliyyat;
using FinNex.Domain.Entities.Emeliyyat;
using FinNex.Domain.Entities.HR;
using FinNex.Application.Services.Hevale;
using FinNex.Domain.Interfaces;

namespace FinNex.Application.Services.Emeliyyat;

public class KocurmeService : IKocurmeService
{
    private readonly IUnitOfWork _uow;

    public KocurmeService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    // Həvalə № prefiksi (BMI: pul köçürməsi "T", tələbə köçürməsi "TL")
    private static string Prefiks(string novu) =>
        string.Equals(novu, "Telebe", StringComparison.OrdinalIgnoreCase) ? "TL" : "T";

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
                YaradanId     = x.YaradanIcraciId
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
            Qeyd             = e.Qeyd
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

        await _uow.Repository<Kocurme>().YaratAsync(e);
        await _uow.YaddaSaxlaAsync();

        return Result<int>.Ok(e.Id, $"Köçürmə qeydə alındı — № {hevaleNo}.");
    }

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
            Qeyd             = e.Qeyd
        };
    }

    public async Task<Result> YenileAsync(string novu, KocurmeEditDto dto, int userId, bool isAdmin)
    {
        var e = await _uow.Repository<Kocurme>().GetirAsync(x => x.Id == dto.Id && x.Novu == novu && !x.Silinib);
        if (e == null) return Result.Fail("Köçürmə tapılmadı.");
        if (!isAdmin && e.YaradanIcraciId != userId)
            return Result.Fail("Yalnız öz qeydinizi və ya Admin dəyişə bilər.");

        Doldur(e, dto);   // HevaleNo dəyişməz
        e.YenileyenIcraciId = userId;
        e.YenilenmeTarixi   = DateTime.Now;

        await _uow.Repository<Kocurme>().YenileAsync(e);
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
    }
}
