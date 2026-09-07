using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Emeliyyat;
using FinNex.Application.Interfaces.Emeliyyat;
using FinNex.Domain.Entities.Emeliyyat;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.Emeliyyat;

/// <inheritdoc cref="IKocurmeSenedNovuService"/>
public class KocurmeSenedNovuService : IKocurmeSenedNovuService
{
    private readonly IUnitOfWork _uow;

    public KocurmeSenedNovuService(IUnitOfWork uow) => _uow = uow;

    public async Task<IList<KocurmeSenedNovuDto>> AktivlerAsync()
        => await Sorgu().Where(x => x.Aktivdir).Select(Map).ToListAsync();

    public async Task<IList<KocurmeSenedNovuDto>> HamisiAsync()
        => await Sorgu().Select(Map).ToListAsync();

    public async Task<Result<int>> YaratAsync(string ad, int userId)
    {
        var temiz = (ad ?? "").Trim();
        if (temiz.Length == 0)
            return Result<int>.Fail("Sənəd növünün adı boş ola bilməz.");
        if (temiz.Length > 200)
            return Result<int>.Fail("Sənəd növünün adı 200 simvoldan uzun ola bilməz.");

        // Dublikat yoxlaması DEAKTİVLƏRİ DƏ sayır: eyni ad ikinci sətir kimi
        // yaranmasın — növ üzrə hesabat ikiyə bölünərdi.
        var movcud = await _uow.Repository<KocurmeSenedNovu>().Query()
            .FirstOrDefaultAsync(x => !x.Silinib && x.Ad.ToUpper() == temiz.ToUpper());

        if (movcud != null)
        {
            if (movcud.Aktivdir)
                return Result<int>.Ok(movcud.Id, "Bu sənəd növü artıq siyahıdadır.");

            // Deaktiv idi — yenidən açılır. Yeni sətir yaratmaqdansa köhnəni
            // qaytarmaq keçmiş köçürmələrin bağını qoruyur.
            movcud.Aktivdir = true;
            movcud.YenileyenIcraciId = userId;
            movcud.YenilenmeTarixi = DateTime.Now;
            await _uow.Repository<KocurmeSenedNovu>().YenileAsync(movcud);
            await _uow.YaddaSaxlaAsync();
            return Result<int>.Ok(movcud.Id, "Sənəd növü yenidən aktivləşdirildi.");
        }

        var sonSira = await _uow.Repository<KocurmeSenedNovu>().Query()
            .Where(x => !x.Silinib)
            .Select(x => (int?)x.Sira)
            .MaxAsync() ?? 0;

        var e = new KocurmeSenedNovu
        {
            Ad = temiz,
            Sira = sonSira + 10,
            Aktivdir = true,
            YaradanIcraciId = userId
        };

        await _uow.Repository<KocurmeSenedNovu>().YaratAsync(e);
        await _uow.YaddaSaxlaAsync();

        return Result<int>.Ok(e.Id, "Sənəd növü əlavə edildi.");
    }

    public async Task<Result> VeziyyetDeyisAsync(int id, int userId)
    {
        var e = await _uow.Repository<KocurmeSenedNovu>()
            .GetirAsync(x => x.Id == id && !x.Silinib);

        if (e == null) return Result.Fail("Sənəd növü tapılmadı.");

        e.Aktivdir = !e.Aktivdir;
        e.YenileyenIcraciId = userId;
        e.YenilenmeTarixi = DateTime.Now;

        await _uow.Repository<KocurmeSenedNovu>().YenileAsync(e);
        await _uow.YaddaSaxlaAsync();

        return Result.Ok(e.Aktivdir ? "Aktivləşdirildi." : "Deaktiv edildi.");
    }

    private IQueryable<KocurmeSenedNovu> Sorgu()
        => _uow.Repository<KocurmeSenedNovu>().Query()
               .Where(x => !x.Silinib)
               .OrderBy(x => x.Sira).ThenBy(x => x.Ad)
               .AsNoTracking();

    private static readonly System.Linq.Expressions.Expression<Func<KocurmeSenedNovu, KocurmeSenedNovuDto>> Map =
        x => new KocurmeSenedNovuDto { Id = x.Id, Ad = x.Ad, Sira = x.Sira, Aktivdir = x.Aktivdir };
}
