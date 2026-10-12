using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Communication;
using FinNex.Application.Interfaces.Communication;
using FinNex.Domain.Entities.Communication;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.Communication;

public class MailIzlemeService : IMailIzlemeService
{
    private readonly IUnitOfWork _uow;

    public MailIzlemeService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    // Pul Köçürməsi modulundakı `FinTemizle` ilə EYNİ prinsip (CLAUDE.md) —
    // yazan və axtaran MÜTLƏQ eyni normallaşdırmadan keçməlidir, yoxsa
    // "Qara@X.com" ilə "qara@x.com" səssizcə iki ayrı ünvan sayılar.
    private static string Normallashdir(string email) => email.Trim().ToLowerInvariant();

    public async Task<List<MailIzlemeUnvaniDto>> GetListAsync(int sahibUserId)
    {
        var list = await _uow.Repository<MailIzlemeUnvani>().Query()
            .AsNoTracking()
            .Where(x => x.SahibUserId == sahibUserId && !x.Silinib)
            .OrderBy(x => x.Ad ?? x.Email)
            .ToListAsync();

        return list.Select(x => new MailIzlemeUnvaniDto
        {
            Id = x.Id,
            Email = x.Email,
            Ad = x.Ad,
            YaradilmaTarixi = x.YaradilmaTarixi
        }).ToList();
    }

    public async Task<Result> EkleAsync(int sahibUserId, string email, string? ad)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Result.Fail("E-poçt ünvanı boş ola bilməz.");

        var temiz = Normallashdir(email);

        try { _ = new System.Net.Mail.MailAddress(temiz); }
        catch { return Result.Fail("Düzgün e-poçt ünvanı daxil edin."); }

        // ⚠️ Unikal indeks (SahibUserId, Email) `Silinib`-i FİLTRLƏMİR (CLAUDE.md:
        // "Yumşaq Silinmiş Sətir + UNİKAL İNDEKS = Səssiz INSERT Xətası") — ünvan
        // silinib yenidən əlavə edilirsə sətir var-NÖVBƏDƏN YARADILA BİLMƏZ,
        // köhnə (silinmiş) sətir DİRİLDİLMƏLİDİR.
        // `Query()` avtomatik `!Silinib` tətbiq edir (CLAUDE.md) — silinmiş sətri
        // görmək üçün burada MƏCBURİ `QueryAll()` lazımdır, yoxsa yuxarıdakı
        // "dirilt" qolu heç vaxt işə düşmür və unikal indeks INSERT-i sındırır.
        var movcud = await _uow.Repository<MailIzlemeUnvani>().QueryAll()
            .FirstOrDefaultAsync(x => x.SahibUserId == sahibUserId && x.Email == temiz);

        if (movcud != null && !movcud.Silinib)
            return Result.Fail("Bu ünvan artıq siyahınızdadır.");

        if (movcud != null)
        {
            movcud.Silinib = false;
            movcud.SilinmeTarixi = null;
            movcud.Ad = string.IsNullOrWhiteSpace(ad) ? null : ad.Trim();
        }
        else
        {
            await _uow.Repository<MailIzlemeUnvani>().YaratAsync(new MailIzlemeUnvani
            {
                SahibUserId = sahibUserId,
                Email = temiz,
                Ad = string.IsNullOrWhiteSpace(ad) ? null : ad.Trim()
            });
        }

        await _uow.YaddaSaxlaAsync();
        return Result.Ok("Ünvan əlavə edildi.");
    }

    public async Task<Result> SilAsync(int id, int sahibUserId)
    {
        var item = await _uow.Repository<MailIzlemeUnvani>().Query()
            .FirstOrDefaultAsync(x => x.Id == id && !x.Silinib);

        if (item == null) return Result.Fail("Tapılmadı.");
        if (item.SahibUserId != sahibUserId) return Result.Fail("Bu ünvan sizə aid deyil.");

        item.Silinib = true;
        item.SilinmeTarixi = DateTime.Now;
        await _uow.YaddaSaxlaAsync();
        return Result.Ok("Ünvan silindi.");
    }

    public async Task<List<string>> GetEmailSiyahisiAsync(int sahibUserId)
    {
        return await _uow.Repository<MailIzlemeUnvani>().Query()
            .AsNoTracking()
            .Where(x => x.SahibUserId == sahibUserId && !x.Silinib)
            .Select(x => x.Email)
            .ToListAsync();
    }
}
