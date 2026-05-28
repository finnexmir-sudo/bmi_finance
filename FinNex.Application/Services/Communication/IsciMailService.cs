using FinNex.Application.DTOs.Communication.IsciMail;
using FinNex.Application.Interfaces.Communication;
using FinNex.Domain.Entities.Communication;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.Communication;

public class IsciMailService : IIsciMailService
{
    private readonly IUnitOfWork _uow;
    private readonly IBildirisRouter _bildirisRouter;

    public IsciMailService(IUnitOfWork uow, IBildirisRouter bildirisRouter)
    {
        _uow = uow;
        _bildirisRouter = bildirisRouter;
    }

    public async Task<int> GonderAsync(IsciMailCreateDto dto, int gonderenIsciId)
    {
        var now = DateTime.Now;

        var mail = new IsciMail
        {
            GonderenIsciId = gonderenIsciId,
            Movzu = dto.Movzu.Trim(),
            Metin = dto.Metin,
            MetinHtml = dto.MetinHtml,
            Taslagdirmi = false,
            GondermeTarixi = now,
            YaradilmaTarixi = now
        };

        foreach (var isciId in dto.AliciIsciIds.Distinct())
            mail.Alicilar.Add(new IsciMailAlici { AliciIsciId = isciId, AliciNovu = "To", YaradilmaTarixi = now });

        foreach (var isciId in dto.CcIsciIds.Distinct().Where(id => !dto.AliciIsciIds.Contains(id)))
            mail.Alicilar.Add(new IsciMailAlici { AliciIsciId = isciId, AliciNovu = "CC", YaradilmaTarixi = now });

        await _uow.Repository<IsciMail>().YaratAsync(mail);
        await _uow.YaddaSaxlaAsync();

        // ── FinNex Agent handshake: hər alıcıya real-time bildiriş ──
        var gonderen = await _uow.Repository<Isci>().Query()
            .AsNoTracking()
            .Where(x => x.Id == gonderenIsciId)
            .Select(x => new { x.Ad, x.Soyad })
            .FirstOrDefaultAsync();

        var gonderenAd = gonderen != null ? $"{gonderen.Ad} {gonderen.Soyad}".Trim() : "Naməlum";
        var movzuKisar = mail.Movzu.Length > 45 ? mail.Movzu[..45] + "..." : mail.Movzu;
        var redirectUrl = $"/User/IsciMail/Detay/{mail.Id}";

        foreach (var alici in mail.Alicilar)
        {
            await _bildirisRouter.NotifyIsciAsync(
                isciId: alici.AliciIsciId,
                nov: BildirisNovu.YeniIsciMail,
                bashliq: $"Yeni daxili mail: {movzuKisar}",
                metn: $"{gonderenAd} sizə yeni mail göndərdi.",
                redirectUrl: redirectUrl);
        }

        return mail.Id;
    }

    public async Task<int> TaslagKaydetAsync(IsciMailCreateDto dto, int gonderenIsciId)
    {
        var now = DateTime.Now;

        var mail = new IsciMail
        {
            GonderenIsciId = gonderenIsciId,
            Movzu = dto.Movzu.Trim(),
            Metin = dto.Metin,
            MetinHtml = dto.MetinHtml,
            Taslagdirmi = true,
            YaradilmaTarixi = now
        };

        foreach (var isciId in dto.AliciIsciIds.Distinct())
            mail.Alicilar.Add(new IsciMailAlici { AliciIsciId = isciId, AliciNovu = "To", YaradilmaTarixi = now });

        foreach (var isciId in dto.CcIsciIds.Distinct().Where(id => !dto.AliciIsciIds.Contains(id)))
            mail.Alicilar.Add(new IsciMailAlici { AliciIsciId = isciId, AliciNovu = "CC", YaradilmaTarixi = now });

        await _uow.Repository<IsciMail>().YaratAsync(mail);
        await _uow.YaddaSaxlaAsync();
        return mail.Id;
    }

    public async Task TaslagiGonderAsync(int mailId, int gonderenIsciId)
    {
        var mail = await _uow.Repository<IsciMail>().Query()
            .Where(x => x.Id == mailId && x.GonderenIsciId == gonderenIsciId && x.Taslagdirmi && !x.Silinib)
            .Include(x => x.Alicilar)
            .FirstOrDefaultAsync();

        if (mail == null) return;

        mail.Taslagdirmi = false;
        mail.GondermeTarixi = DateTime.Now;
        await _uow.YaddaSaxlaAsync();

        var gonderen = await _uow.Repository<Isci>().Query()
            .AsNoTracking()
            .Where(x => x.Id == gonderenIsciId)
            .Select(x => new { x.Ad, x.Soyad })
            .FirstOrDefaultAsync();

        var gonderenAd = gonderen != null ? $"{gonderen.Ad} {gonderen.Soyad}".Trim() : "Naməlum";
        var movzuKisar = mail.Movzu.Length > 45 ? mail.Movzu[..45] + "..." : mail.Movzu;

        foreach (var alici in mail.Alicilar.Where(a => !a.Silinib))
        {
            await _bildirisRouter.NotifyIsciAsync(
                isciId: alici.AliciIsciId,
                nov: BildirisNovu.YeniIsciMail,
                bashliq: $"Yeni daxili mail: {movzuKisar}",
                metn: $"{gonderenAd} sizə yeni mail göndərdi.",
                redirectUrl: $"/User/IsciMail/Detay/{mail.Id}");
        }
    }

    public async Task<List<IsciMailDto>> InboxGetirAsync(int isciId)
    {
        var records = await _uow.Repository<IsciMailAlici>().Query()
            .AsNoTracking()
            .Where(a => a.AliciIsciId == isciId && !a.Silinib && !a.IsciTarafindenSilindi)
            .Include(a => a.IsciMail).ThenInclude(m => m.Gonderen)
            .Where(a => !a.IsciMail.Taslagdirmi && !a.IsciMail.Silinib)
            .OrderByDescending(a => a.IsciMail.GondermeTarixi)
            .ToListAsync();

        return records.Select(a => new IsciMailDto
        {
            Id = a.IsciMailId,
            GonderenIsciId = a.IsciMail.GonderenIsciId,
            GonderenAdSoyad = a.IsciMail.Gonderen != null
                ? $"{a.IsciMail.Gonderen.Ad} {a.IsciMail.Gonderen.Soyad}".Trim()
                : "",
            Movzu = a.IsciMail.Movzu,
            MetinKisar = a.IsciMail.Metin.Length > 100 ? a.IsciMail.Metin[..100] + "..." : a.IsciMail.Metin,
            GondermeTarixi = a.IsciMail.GondermeTarixi,
            YaradilmaTarixi = a.IsciMail.YaradilmaTarixi,
            Oxunub = a.Oxunub,
            Taslagdirmi = false,
            AliciSayi = 0
        }).ToList();
    }

    public async Task<List<IsciMailDto>> GonderilenlerGetirAsync(int isciId)
    {
        var mails = await _uow.Repository<IsciMail>().Query()
            .AsNoTracking()
            .Where(m => m.GonderenIsciId == isciId && !m.Taslagdirmi && !m.Silinib)
            .Include(m => m.Alicilar.Where(a => !a.Silinib))
            .OrderByDescending(m => m.GondermeTarixi)
            .ToListAsync();

        return mails.Select(m => new IsciMailDto
        {
            Id = m.Id,
            GonderenIsciId = m.GonderenIsciId,
            GonderenAdSoyad = "",
            Movzu = m.Movzu,
            MetinKisar = m.Metin.Length > 100 ? m.Metin[..100] + "..." : m.Metin,
            GondermeTarixi = m.GondermeTarixi,
            YaradilmaTarixi = m.YaradilmaTarixi,
            Oxunub = true,
            Taslagdirmi = false,
            AliciSayi = m.Alicilar.Count
        }).ToList();
    }

    public async Task<List<IsciMailDto>> TaslaqlarGetirAsync(int isciId)
    {
        var mails = await _uow.Repository<IsciMail>().Query()
            .AsNoTracking()
            .Where(m => m.GonderenIsciId == isciId && m.Taslagdirmi && !m.Silinib)
            .Include(m => m.Alicilar.Where(a => !a.Silinib))
            .OrderByDescending(m => m.YaradilmaTarixi)
            .ToListAsync();

        return mails.Select(m => new IsciMailDto
        {
            Id = m.Id,
            GonderenIsciId = m.GonderenIsciId,
            GonderenAdSoyad = "",
            Movzu = string.IsNullOrEmpty(m.Movzu) ? "(Mövzu yoxdur)" : m.Movzu,
            MetinKisar = m.Metin.Length > 100 ? m.Metin[..100] + "..." : m.Metin,
            GondermeTarixi = null,
            YaradilmaTarixi = m.YaradilmaTarixi,
            Oxunub = true,
            Taslagdirmi = true,
            AliciSayi = m.Alicilar.Count
        }).ToList();
    }

    public async Task<IsciMailDetayDto?> DetayGetirAsync(int mailId, int isciId)
    {
        var mail = await _uow.Repository<IsciMail>().Query()
            .AsNoTracking()
            .Where(m => m.Id == mailId && !m.Silinib)
            .Include(m => m.Gonderen)
            .Include(m => m.Alicilar.Where(a => !a.Silinib))
                .ThenInclude(a => a.Alici)
            .FirstOrDefaultAsync();

        if (mail == null) return null;

        bool isAlici = mail.Alicilar.Any(a => a.AliciIsciId == isciId);
        bool isGonderen = mail.GonderenIsciId == isciId;

        if (!isAlici && !isGonderen) return null;

        if (isAlici)
        {
            var aliciRecord = await _uow.Repository<IsciMailAlici>().Query()
                .Where(a => a.IsciMailId == mailId && a.AliciIsciId == isciId && !a.Silinib)
                .FirstOrDefaultAsync();

            if (aliciRecord != null && !aliciRecord.Oxunub)
            {
                aliciRecord.Oxunub = true;
                aliciRecord.OxunmaTarixi = DateTime.Now;
                await _uow.YaddaSaxlaAsync();
            }
        }

        return new IsciMailDetayDto
        {
            Id = mail.Id,
            GonderenIsciId = mail.GonderenIsciId,
            GonderenAdSoyad = mail.Gonderen != null
                ? $"{mail.Gonderen.Ad} {mail.Gonderen.Soyad}".Trim()
                : "",
            Movzu = mail.Movzu,
            Metin = mail.Metin,
            MetinHtml = mail.MetinHtml,
            GondermeTarixi = mail.GondermeTarixi,
            YaradilmaTarixi = mail.YaradilmaTarixi,
            Taslagdirmi = mail.Taslagdirmi,
            Alicilar = mail.Alicilar.Select(a => new IsciMailAliciDto
            {
                IsciId = a.AliciIsciId,
                AdSoyad = a.Alici != null ? $"{a.Alici.Ad} {a.Alici.Soyad}".Trim() : "",
                AliciNovu = a.AliciNovu,
                Oxunub = a.Oxunub,
                OxunmaTarixi = a.OxunmaTarixi
            }).ToList()
        };
    }

    public async Task SilAsync(int mailId, int isciId)
    {
        var mail = await _uow.Repository<IsciMail>().Query()
            .Where(m => m.Id == mailId && m.GonderenIsciId == isciId && !m.Silinib)
            .FirstOrDefaultAsync();

        if (mail != null)
        {
            mail.Silinib = true;
            mail.SilinmeTarixi = DateTime.Now;
            await _uow.YaddaSaxlaAsync();
            return;
        }

        var aliciRecord = await _uow.Repository<IsciMailAlici>().Query()
            .Where(a => a.IsciMailId == mailId && a.AliciIsciId == isciId && !a.Silinib)
            .FirstOrDefaultAsync();

        if (aliciRecord != null)
        {
            aliciRecord.IsciTarafindenSilindi = true;
            aliciRecord.YenilenmeTarixi = DateTime.Now;
            await _uow.YaddaSaxlaAsync();
        }
    }

    public async Task<int> OxunmamisSayiAsync(int isciId)
    {
        return await _uow.Repository<IsciMailAlici>().Query()
            .AsNoTracking()
            .CountAsync(a =>
                a.AliciIsciId == isciId
                && !a.Oxunub
                && !a.Silinib
                && !a.IsciTarafindenSilindi
                && !a.IsciMail.Taslagdirmi
                && !a.IsciMail.Silinib);
    }
}
