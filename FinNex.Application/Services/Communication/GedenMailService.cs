using FinNex.Application.DTOs.Communication;
using FinNex.Application.Interfaces.Communication;
using FinNex.Domain.Entities.Communication;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.Communication;

public class GedenMailService : IGedenMailService
{
    private readonly IUnitOfWork _uow;

    public GedenMailService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<List<GedenMailListDto>> GetListAsync(int? sahibUserId = null, string? axtaris = null, DateTime? tarixden = null, DateTime? tarixe = null, bool? qosmali = null, int page = 1, int pageSize = 50, List<string>? izlenenEmaillar = null)
    {
        IQueryable<GedenMail> query = _uow.Repository<GedenMail>().Query()
            .AsNoTracking()
            .Where(x => !x.Silinib)
            .Include(x => x.Qosmalar);

        // İzolyasiya — GelenMail ilə EYNİ qayda: yalnız bu sahibin qutusu
        if (sahibUserId.HasValue)
            query = query.Where(x => x.SahibUserId == sahibUserId.Value);

        if (!string.IsNullOrWhiteSpace(axtaris))
        {
            var q = axtaris.Trim().ToLower();
            query = query.Where(x =>
                x.Movzu.ToLower().Contains(q) ||
                x.KimeAd.ToLower().Contains(q) ||
                x.KimeEmail.ToLower().Contains(q) ||
                x.KimeToplu.ToLower().Contains(q));
        }
        if (tarixden.HasValue)
            query = query.Where(x => x.GonderilmeTarixi >= tarixden.Value);
        if (tarixe.HasValue)
            query = query.Where(x => x.GonderilmeTarixi < tarixe.Value.AddDays(1));
        if (qosmali == true)
            query = query.Where(x => x.Qosmalar.Any(q => !q.Silinib));
        // Mail İzləmə Siyahısı (12.10.2026) — "yalnız bu ünvanlara gedənlər".
        // ⚠️ Yalnız İLK (əsas) alıcı (`KimeEmail`) yoxlanılır, `KimeToplu`-dakı
        // Cc-lər YOX — sətir daxilində dinamik siyahı üzrə LIKE SQL-ə yaxşı
        // tərcümə olunmur. Gələn Mail-in simmetrik tərəfi (`KimdenEmail`) tək
        // sahə olduğu üçün bu məhdudiyyətə düşmür.
        // ⚠️ 12.10.2026, DÜZƏLİŞ: `null` ilə BOŞ siyahı FƏRQLİ mənalar daşımalıdır —
        // bax GelenMailService.GetListAsync-dəki eyni düzəliş üçün izah. `null` =
        // filtr tələb olunmayıb. BOŞ (non-null, Count=0) = filtr tələb olunub,
        // amma izləmə siyahısı boşdur → HEÇ NƏ uyğun gəlməməlidir, "hamısı" YOX.
        if (izlenenEmaillar != null)
        {
            query = izlenenEmaillar.Count > 0
                ? query.Where(x => izlenenEmaillar.Contains(x.KimeEmail.ToLower()))
                : query.Where(x => false);
        }

        var mails = await query
            .OrderByDescending(x => x.GonderilmeTarixi)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return mails.Select(m => new GedenMailListDto
        {
            Id = m.Id,
            KimeAd = m.KimeAd,
            KimeEmail = m.KimeEmail,
            KimeToplu = m.KimeToplu,
            Movzu = m.Movzu,
            GonderilmeTarixi = m.GonderilmeTarixi,
            QosmaCount = m.Qosmalar.Count(q => !q.Silinib)
        }).ToList();
    }

    public async Task<GedenMailDetailDto?> GetDetailAsync(int id, int? sahibUserId = null)
    {
        var query = _uow.Repository<GedenMail>().Query()
            .AsNoTracking()
            .Where(x => x.Id == id && !x.Silinib);

        // İzolyasiya — sahib verilibsə yalnız onun maili açıla bilər (URL ilə sızmanın qarşısı)
        if (sahibUserId.HasValue)
            query = query.Where(x => x.SahibUserId == sahibUserId.Value);

        var m = await query
            .Include(x => x.Qosmalar)
            .FirstOrDefaultAsync();

        if (m == null) return null;

        return new GedenMailDetailDto
        {
            Id = m.Id,
            MessageId = m.MessageId,
            KimeAd = m.KimeAd,
            KimeEmail = m.KimeEmail,
            KimeToplu = m.KimeToplu,
            Movzu = m.Movzu,
            MetinHtml = m.MetinHtml,
            MetinDuz = m.MetinDuz,
            GonderilmeTarixi = m.GonderilmeTarixi,
            Qosmalar = m.Qosmalar.Where(q => !q.Silinib).Select(q => new GedenMailQosmaDto
            {
                Id = q.Id,
                FaylAdi = q.FaylAdi,
                ContentType = q.ContentType,
                OlcuBayt = q.OlcuBayt,
                CixarilmisMetin = q.CixarilmisMetin
            }).ToList()
        };
    }

    public async Task SilAsync(int id, int? sahibUserId = null)
    {
        var mail = await _uow.Repository<GedenMail>().Query()
            .Where(x => x.Id == id && !x.Silinib)
            .FirstOrDefaultAsync();
        if (mail == null) return;
        if (sahibUserId.HasValue && mail.SahibUserId != sahibUserId.Value) return;
        mail.Silinib = true;
        mail.SilinmeTarixi = DateTime.Now;
        await _uow.YaddaSaxlaAsync();
    }
}
