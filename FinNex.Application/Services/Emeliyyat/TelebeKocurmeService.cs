using System.Globalization;
using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Emeliyyat;
using FinNex.Application.Interfaces.Emeliyyat;
using FinNex.Domain.Entities.Emeliyyat;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;

namespace FinNex.Application.Services.Emeliyyat;

public class TelebeKocurmeService : ITelebeKocurmeService
{
    private readonly IUnitOfWork _uow;

    public TelebeKocurmeService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    // Standart hesab nömrələri (BMI Telebe formasındakı default-lar)
    public (string h35025, string h45023, string h45011, string h67013) StandartHesablar()
        => ("35025010000001600000", "45023010020000400000", "45011000010000400000", "67013000000000600000");

    // Komissiya = Mebleg × Kurs × XH / 100, minimum 0.5 (BMI məntiqi)
    public decimal KomissiyaHesabla(decimal? mebleg, decimal? kurs, decimal? xh)
    {
        var m = mebleg ?? 0m;
        var k = kurs ?? 0m;
        var x = xh ?? 0m;
        var komissiya = m * k * x / 100m;
        return komissiya >= 0.5m ? komissiya : 0.5m;
    }

    private async Task<Dictionary<int, string?>> IcraciAdMapAsync()
    {
        var isciler = await _uow.Repository<Isci>().HamisiniGetirAsync(
            predicate: x => !x.Silinib && x.IcraciNo != null, izlemeden: true);
        return isciler.Where(i => i.IcraciNo.HasValue)
            .GroupBy(i => i.IcraciNo!.Value)
            .ToDictionary(g => g.Key, g => g.First().TamAd);
    }

    public async Task<IList<TelebeKocurmeListDto>> HamisiniGetirAsync(int? il = null)
    {
        var list = await _uow.Repository<TelebeKocurme>().HamisiniGetirAsync(
            predicate: x => !x.Silinib && (il == null || (x.Tarix != null && x.Tarix.Value.Year == il)),
            izlemeden: true);

        var adMap = await IcraciAdMapAsync();

        return list
            .OrderByDescending(x => x.Tarix).ThenByDescending(x => x.Id)
            .Select(x => new TelebeKocurmeListDto
            {
                Id        = x.Id,
                HevaleNo  = x.HevaleNo,
                Tarix     = x.Tarix,
                Adi       = x.Adi,
                UniAd     = x.UniAd,
                Mebleg    = x.Mebleg,
                Komissiya = x.Komissiya,
                AlanBank  = x.AlanBank,
                Icra      = x.Icra,
                IcraciAd  = (x.Icra.HasValue && adMap.TryGetValue(x.Icra.Value, out var ad)) ? ad : null,
                YaradanId = x.YaradanIcraciId
            })
            .ToList();
    }

    // 3 debet/kredit muhasibat sətri (BMI məntiqi)
    private static IList<MuhasibatSetirDto> Setirler(TelebeKocurme e, decimal komissiya)
    {
        var kursStr = (e.Kurs ?? 0m).ToString(CultureInfo.InvariantCulture);
        var t1 = $" G/H {e.HevaleNo} REF {e.RefNo} BMI {e.BmiFilial} {e.AlanBank} Tranz. {e.UniAd} t/h {e.TelebeKursu} -kurs {e.Adi} {e.Passport}";
        var t2 = $"1USD={kursStr} AZN  G/H {e.HevaleNo} {e.Adi} -{e.UniAd} kurs {e.TelebeKursu} təh/h pasp {e.Passport}";
        var t3 = $" G/H {e.HevaleNo} REF {e.RefNo} BMI {e.BmiFilial} {e.AlanBank} Tranz. {e.UniAd} t/h {e.TelebeKursu} -kurs {e.Adi} {e.Passport} x/h ";
        var m = e.Mebleg ?? 0m;
        return new List<MuhasibatSetirDto>
        {
            new() { Debet = e.Hes35025, Kredit = e.Hes45023, Mebleg = m,         Teyinat = t1 },
            new() { Debet = e.Hes45023, Kredit = e.Hes45011, Mebleg = m,         Teyinat = t2 },
            new() { Debet = e.Hes45011, Kredit = e.Hes67013, Mebleg = komissiya, Teyinat = t3 },
        };
    }

    public IList<MuhasibatSetirDto> SetirlerHesabla(TelebeKocurmeFormDto dto)
    {
        var (h35025, h45023, h45011, h67013) = StandartHesablar();
        var e = new TelebeKocurme
        {
            HevaleNo    = dto.HevaleNo,
            Adi         = dto.Adi,
            Passport    = dto.Passport,
            Mebleg      = dto.Mebleg,
            BmiFilial   = dto.BmiFilial,
            RefNo       = dto.RefNo,
            UniAd       = dto.UniAd,
            AlanBank    = string.IsNullOrWhiteSpace(dto.AlanBank) ? "Kapital" : dto.AlanBank,
            TelebeKursu = dto.TelebeKursu,
            XH          = dto.XH ?? 0.1m,
            Kurs        = dto.Kurs ?? 1.68m,
            Hes35025    = string.IsNullOrWhiteSpace(dto.Hes35025) ? h35025 : dto.Hes35025,
            Hes45023    = string.IsNullOrWhiteSpace(dto.Hes45023) ? h45023 : dto.Hes45023,
            Hes45011    = string.IsNullOrWhiteSpace(dto.Hes45011) ? h45011 : dto.Hes45011,
            Hes67013    = string.IsNullOrWhiteSpace(dto.Hes67013) ? h67013 : dto.Hes67013
        };
        var komissiya = KomissiyaHesabla(e.Mebleg, e.Kurs, e.XH);
        return Setirler(e, komissiya);
    }

    public async Task<TelebeKocurmeDetalDto?> DetalAsync(int id)
    {
        var e = await _uow.Repository<TelebeKocurme>().GetirAsync(x => x.Id == id && !x.Silinib, izlemeden: true);
        if (e == null) return null;

        var komissiya = e.Komissiya ?? KomissiyaHesabla(e.Mebleg, e.Kurs, e.XH);
        return new TelebeKocurmeDetalDto
        {
            Id          = e.Id,
            HevaleNo    = e.HevaleNo,
            Tarix       = e.Tarix,
            Adi         = e.Adi,
            Passport    = e.Passport,
            UniAd       = e.UniAd,
            AlanBank    = e.AlanBank,
            BmiFilial   = e.BmiFilial,
            RefNo       = e.RefNo,
            TelebeKursu = e.TelebeKursu,
            Mebleg      = e.Mebleg,
            Kurs        = e.Kurs,
            XH          = e.XH,
            Komissiya   = komissiya,
            YaradanId   = e.YaradanIcraciId,
            Setirler    = Setirler(e, komissiya)
        };
    }

    public async Task<Result<int>> YaratAsync(TelebeKocurmeCreateDto dto, int yaradanUserId)
    {
        if (string.IsNullOrWhiteSpace(dto.Adi))
            return Result<int>.Fail("Tələbənin adı boş ola bilməz.");
        if (dto.Mebleg is null or <= 0)
            return Result<int>.Fail("Məbləğ düzgün daxil edilməlidir.");

        var isci = (await _uow.Repository<Isci>().HamisiniGetirAsync(
            predicate: x => x.AppUserId == yaradanUserId && !x.Silinib, izlemeden: true)).FirstOrDefault();
        short? icraNo = (isci?.IcraciNo is int no && no > 0 && no <= short.MaxValue) ? (short)no : (short?)null;

        var (h35025, h45023, h45011, h67013) = StandartHesablar();

        var e = new TelebeKocurme
        {
            HevaleNo    = dto.HevaleNo?.Trim(),
            Tarix       = dto.Tarix ?? DateTime.Now,
            Adi         = dto.Adi?.Trim(),
            Passport    = dto.Passport?.Trim(),
            Mebleg      = dto.Mebleg,
            BmiFilial   = dto.BmiFilial?.Trim(),
            RefNo       = dto.RefNo?.Trim(),
            UniAd       = dto.UniAd?.Trim(),
            AlanBank    = string.IsNullOrWhiteSpace(dto.AlanBank) ? "Kapital" : dto.AlanBank.Trim(),
            TelebeKursu = dto.TelebeKursu?.Trim(),
            XH          = dto.XH ?? 0.1m,
            Kurs        = dto.Kurs ?? 1.68m,
            Hes35025    = string.IsNullOrWhiteSpace(dto.Hes35025) ? h35025 : dto.Hes35025.Trim(),
            Hes45023    = string.IsNullOrWhiteSpace(dto.Hes45023) ? h45023 : dto.Hes45023.Trim(),
            Hes45011    = string.IsNullOrWhiteSpace(dto.Hes45011) ? h45011 : dto.Hes45011.Trim(),
            Hes67013    = string.IsNullOrWhiteSpace(dto.Hes67013) ? h67013 : dto.Hes67013.Trim(),
            Icra        = icraNo,
            YaradanIcraciId = yaradanUserId
        };
        e.Komissiya = KomissiyaHesabla(e.Mebleg, e.Kurs, e.XH);

        await _uow.Repository<TelebeKocurme>().YaratAsync(e);
        await _uow.YaddaSaxlaAsync();

        return Result<int>.Ok(e.Id, $"Tələbə köçürməsi qeydə alındı — komissiya {e.Komissiya:#,0.00}.");
    }

    public async Task<TelebeKocurmeCreateDto?> TekrarMelumatiAsync(int id)
    {
        var e = await _uow.Repository<TelebeKocurme>().GetirAsync(x => x.Id == id && !x.Silinib, izlemeden: true);
        if (e == null) return null;

        return new TelebeKocurmeCreateDto
        {
            Tarix       = DateTime.Today,
            HevaleNo    = null,   // yeni № əl ilə
            Adi         = e.Adi,
            Passport    = e.Passport,
            Mebleg      = e.Mebleg,
            BmiFilial   = e.BmiFilial,
            RefNo       = e.RefNo,
            UniAd       = e.UniAd,
            AlanBank    = e.AlanBank,
            TelebeKursu = e.TelebeKursu,
            XH          = e.XH,
            Kurs        = e.Kurs,
            Hes35025    = e.Hes35025,
            Hes45023    = e.Hes45023,
            Hes45011    = e.Hes45011,
            Hes67013    = e.Hes67013
        };
    }

    public async Task<TelebeKocurmeEditDto?> RedakteMelumatiAsync(int id)
    {
        var e = await _uow.Repository<TelebeKocurme>().GetirAsync(x => x.Id == id && !x.Silinib, izlemeden: true);
        if (e == null) return null;

        return new TelebeKocurmeEditDto
        {
            Id          = e.Id,
            YaradanId   = e.YaradanIcraciId,
            Tarix       = e.Tarix,
            HevaleNo    = e.HevaleNo,
            Adi         = e.Adi,
            Passport    = e.Passport,
            Mebleg      = e.Mebleg,
            BmiFilial   = e.BmiFilial,
            RefNo       = e.RefNo,
            UniAd       = e.UniAd,
            AlanBank    = e.AlanBank,
            TelebeKursu = e.TelebeKursu,
            XH          = e.XH,
            Kurs        = e.Kurs,
            Hes35025    = e.Hes35025,
            Hes45023    = e.Hes45023,
            Hes45011    = e.Hes45011,
            Hes67013    = e.Hes67013
        };
    }

    public async Task<Result> YenileAsync(TelebeKocurmeEditDto dto, int userId, bool isAdmin)
    {
        if (string.IsNullOrWhiteSpace(dto.Adi))
            return Result.Fail("Tələbənin adı boş ola bilməz.");
        if (dto.Mebleg is null or <= 0)
            return Result.Fail("Məbləğ düzgün daxil edilməlidir.");

        var e = await _uow.Repository<TelebeKocurme>().GetirAsync(x => x.Id == dto.Id && !x.Silinib);
        if (e == null) return Result.Fail("Qeyd tapılmadı.");
        if (!isAdmin && e.YaradanIcraciId != userId)
            return Result.Fail("Yalnız öz qeydinizi və ya Admin dəyişə bilər.");

        e.Tarix       = dto.Tarix;
        e.HevaleNo    = dto.HevaleNo?.Trim();
        e.Adi         = dto.Adi?.Trim();
        e.Passport    = dto.Passport?.Trim();
        e.Mebleg      = dto.Mebleg;
        e.BmiFilial   = dto.BmiFilial?.Trim();
        e.RefNo       = dto.RefNo?.Trim();
        e.UniAd       = dto.UniAd?.Trim();
        e.AlanBank    = string.IsNullOrWhiteSpace(dto.AlanBank) ? "Kapital" : dto.AlanBank.Trim();
        e.TelebeKursu = dto.TelebeKursu?.Trim();
        e.XH          = dto.XH ?? 0.1m;
        e.Kurs        = dto.Kurs ?? 1.68m;
        e.Hes35025    = dto.Hes35025?.Trim();
        e.Hes45023    = dto.Hes45023?.Trim();
        e.Hes45011    = dto.Hes45011?.Trim();
        e.Hes67013    = dto.Hes67013?.Trim();
        e.Komissiya   = KomissiyaHesabla(e.Mebleg, e.Kurs, e.XH);
        e.YenileyenIcraciId = userId;
        e.YenilenmeTarixi   = DateTime.Now;

        await _uow.Repository<TelebeKocurme>().YenileAsync(e);
        await _uow.YaddaSaxlaAsync();
        return Result.Ok("Tələbə köçürməsi yeniləndi.");
    }

    public async Task<Result> SilAsync(int id, int userId, bool isAdmin)
    {
        var e = await _uow.Repository<TelebeKocurme>().GetirAsync(x => x.Id == id && !x.Silinib);
        if (e == null) return Result.Fail("Qeyd tapılmadı.");
        if (!isAdmin && e.YaradanIcraciId != userId)
            return Result.Fail("Yalnız öz qeydinizi və ya Admin silə bilər.");

        e.Silinib       = true;
        e.SilinmeTarixi = DateTime.Now;
        e.SilenIcraciId = userId;

        await _uow.Repository<TelebeKocurme>().YenileAsync(e);
        await _uow.YaddaSaxlaAsync();
        return Result.Ok("Tələbə köçürməsi silindi.");
    }
}
