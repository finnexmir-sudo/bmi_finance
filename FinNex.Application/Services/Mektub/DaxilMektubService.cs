using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Mektub;
using FinNex.Application.Interfaces.Mektub;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Entities.Mektub;
using FinNex.Domain.Interfaces;

namespace FinNex.Application.Services.Mektub;

public class DaxilMektubService : IDaxilMektubService
{
    private readonly IUnitOfWork _uow;

    public DaxilMektubService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    // Jurnalda mövcud illər və icraçı nömrələri (filtr açılan siyahıları üçün).
    public async Task<MektubFiltrMenbeDto> FiltrMenbeleriAsync()
    {
        var hamisi = await _uow.Repository<DaxilMektub>()
            .HamisiniGetirAsync(x => !x.Silinib, izlemeden: true);

        var adMap = await IcraciAdXeritesiAsync();

        return new MektubFiltrMenbeDto
        {
            Iller = hamisi.Where(x => x.Il.HasValue).Select(x => x.Il!.Value)
                          .Distinct().OrderByDescending(x => x).ToList(),

            Icracilar = hamisi
                .Where(x => x.MekUnvan.HasValue)
                .GroupBy(x => x.MekUnvan!.Value)
                .Select(g => new MektubIcraciDto
                {
                    No  = g.Key,
                    Ad  = adMap.TryGetValue(g.Key, out var ad) ? ad : null,
                    Say = g.Count()
                })
                .OrderBy(x => x.No)
                .ToList()
        };
    }

    private async Task<Dictionary<int, string>> IcraciAdXeritesiAsync()
    {
        var isciler = await _uow.Repository<Isci>().HamisiniGetirAsync(
            predicate: x => !x.Silinib && x.IcraciNo != null, izlemeden: true);
        return isciler.Where(i => i.IcraciNo.HasValue)
            .GroupBy(i => i.IcraciNo!.Value)
            .ToDictionary(g => g.Key, g => g.First().TamAd);
    }

    public async Task<MektubSehifeDto<DaxilMektubListDto>> HamisiniGetirAsync(MektubFiltrDto? filtr = null)
    {
        var f  = MektubFiltrDto.Normalla(filtr);
        var il = f.SorguIli;   // "bütün illər" seçilibsə null

        // İl, icraçı və tarix DB tərəfində süzülür (54 min sətir yaddaşa çəkilməsin);
        // yalnız mətn axtarışı yaddaşdadır. Tarix filtri DAX_TARIX (daxil olma) üzrədir.
        var list = await _uow.Repository<DaxilMektub>().HamisiniGetirAsync(
            predicate: x => !x.Silinib
                         && (il == null || x.Il == il)
                         && (f.IcraciNo == null || x.MekUnvan == f.IcraciNo)
                         && (f.TarixFrom == null || (x.DaxTarix != null && x.DaxTarix >= f.TarixFrom))
                         && (f.TarixTo   == null || (x.DaxTarix != null && x.DaxTarix <= f.TarixTo)),
            izlemeden: true);

        if (!string.IsNullOrWhiteSpace(f.Axtaris))
        {
            var q = f.Axtaris.Trim();
            list = list.Where(x =>
                    (x.IdareAdi ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (x.DaxNom   ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (x.Nom1     ?? 0).ToString().Contains(q, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        // İcraçı nömrəsi → işçi adı (Isci.IcraciNo)
        var adMap = await IcraciAdXeritesiAsync();

        // Səhifələmə süzgəclərdən SONRA — "Cəmi" filtrə uyğun sətir sayını göstərməlidir.
        var cemi = list.Count;

        return new MektubSehifeDto<DaxilMektubListDto>
        {
            CemiSay      = cemi,
            Sehife       = f.Sehife,
            SehifeOlcusu = f.SehifeOlcusu,
            Setirler     = list
            .OrderByDescending(x => x.Il).ThenByDescending(x => x.Nom1)
            .Skip((f.Sehife - 1) * f.SehifeOlcusu)
            .Take(f.SehifeOlcusu)
            .Select(x => new DaxilMektubListDto
            {
                Id       = x.Id,
                Nom1     = x.Nom1,
                DaxTarix = x.DaxTarix,
                IdareAdi = x.IdareAdi,
                GonTarix = x.GonTarix,
                DaxNom   = x.DaxNom,
                Il       = x.Il,
                IcraciNo = x.MekUnvan,
                IcraciAd = (x.MekUnvan.HasValue && adMap.TryGetValue(x.MekUnvan.Value, out var ad)) ? ad : null,
                YaradanId = x.YaradanIcraciId,
                FaylYolu = x.FaylYolu,
                FaylVar  = !string.IsNullOrEmpty(x.FaylYolu) || (x.Mezmun != null && x.Mezmun.Length > 0)
            })
            .ToList()
        };
    }

    public async Task<Result<int>> YaratAsync(DaxilMektubCreateDto dto, int yaradanUserId, string? faylYolu = null)
    {
        // İcraçı nömrəsi — cari istifadəçinin işçisindən (Isci.AppUserId → IcraciNo)
        var isci = (await _uow.Repository<Isci>().HamisiniGetirAsync(
            predicate: x => x.AppUserId == yaradanUserId && !x.Silinib, izlemeden: true)).FirstOrDefault();
        var icraciNo = isci?.IcraciNo;

        var il = dto.DaxTarix?.Year ?? DateTime.Now.Year;

        // İl üzrə növbəti Qeydiyyat № (yüklənən data + yeni qeydlərdən max+1)
        var heminIl = await _uow.Repository<DaxilMektub>().HamisiniGetirAsync(
            predicate: x => !x.Silinib && x.Il == il, izlemeden: true);
        var novbeti = heminIl.Where(x => x.Nom1.HasValue)
            .Select(x => x.Nom1!.Value)
            .DefaultIfEmpty(0)
            .Max() + 1;

        var entity = new DaxilMektub
        {
            Nom      = novbeti,
            Nom1     = novbeti,
            DaxTarix = dto.DaxTarix,
            IdareAdi = dto.IdareAdi?.Trim(),
            GonTarix = dto.GonTarix,
            DaxNom   = dto.DaxNom?.Trim(),
            MekUnvan = (icraciNo.HasValue && icraciNo.Value > 0) ? icraciNo : (int?)null,
            Il       = il,
            FaylYolu = string.IsNullOrWhiteSpace(faylYolu) ? null : faylYolu,
            YaradanIcraciId = yaradanUserId
        };

        await _uow.Repository<DaxilMektub>().YaratAsync(entity);
        await _uow.YaddaSaxlaAsync();

        return Result<int>.Ok(novbeti, $"Məktub qeydə alındı — Qeydiyyat № {novbeti}/{il}.");
    }

    public async Task<DaxilMektubEditDto?> RedakteMelumatiAsync(int id)
    {
        var e = await _uow.Repository<DaxilMektub>().GetirAsync(
            x => x.Id == id && !x.Silinib, izlemeden: true);
        if (e == null) return null;

        return new DaxilMektubEditDto
        {
            Id             = e.Id,
            DaxTarix       = e.DaxTarix,
            IdareAdi       = e.IdareAdi,
            GonTarix       = e.GonTarix,
            DaxNom         = e.DaxNom,
            Nom1           = e.Nom1,
            Il             = e.Il,
            MovcudFaylYolu = e.FaylYolu,
            YaradanId      = e.YaradanIcraciId
        };
    }

    public async Task<Result> YenileAsync(DaxilMektubEditDto dto, int userId, bool isAdmin, string? yeniFaylYolu = null)
    {
        if (string.IsNullOrWhiteSpace(dto.IdareAdi))
            return Result.Fail("Göndərən idarə/təşkilat adı boş ola bilməz.");

        var e = await _uow.Repository<DaxilMektub>().GetirAsync(x => x.Id == dto.Id && !x.Silinib);
        if (e == null) return Result.Fail("Məktub tapılmadı.");
        if (!isAdmin && e.YaradanIcraciId != userId)
            return Result.Fail("Yalnız öz qeydinizi və ya Admin dəyişə bilər.");

        // Qeydiyyat № (Nom1) və İl dəyişməz qalır — jurnal nömrəsi yaradılışda təyin olunur
        e.DaxTarix = dto.DaxTarix;
        e.IdareAdi = dto.IdareAdi?.Trim();
        e.GonTarix = dto.GonTarix;
        e.DaxNom   = dto.DaxNom?.Trim();
        if (!string.IsNullOrWhiteSpace(yeniFaylYolu))
            e.FaylYolu = yeniFaylYolu;
        e.YenileyenIcraciId = userId;
        e.YenilenmeTarixi   = DateTime.Now;

        await _uow.Repository<DaxilMektub>().YenileAsync(e);
        await _uow.YaddaSaxlaAsync();
        return Result.Ok("Məktub yeniləndi.");
    }

    public async Task<Result> SilAsync(int id, int userId, bool isAdmin)
    {
        var e = await _uow.Repository<DaxilMektub>().GetirAsync(x => x.Id == id && !x.Silinib);
        if (e == null) return Result.Fail("Məktub tapılmadı.");
        if (!isAdmin && e.YaradanIcraciId != userId)
            return Result.Fail("Yalnız öz qeydinizi və ya Admin silə bilər.");

        e.Silinib       = true;
        e.SilinmeTarixi = DateTime.Now;
        e.SilenIcraciId = userId;

        await _uow.Repository<DaxilMektub>().YenileAsync(e);
        await _uow.YaddaSaxlaAsync();
        return Result.Ok("Məktub silindi.");
    }
}
