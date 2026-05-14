using FinNex.Application.DTOs.Communication;
using FinNex.Application.Interfaces.Communication;
using FinNex.Domain.Entities.Communication;
using FinNex.Domain.Entities.SenedDovriyyesi;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.Communication;

public class GelenMailService : IGelenMailService
{
    private readonly IUnitOfWork _uow;

    public GelenMailService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<List<GelenMailListDto>> GetListAsync(bool? oxunmamis = null, int? tapalanIsciId = null, int page = 1, int pageSize = 50)
    {
        var query = _uow.Repository<GelenMail>().Query()
            .AsNoTracking()
            .Where(x => !x.Silinib)
            .Include(x => x.TapalanIsci)
            .Include(x => x.Qosmalar);

        if (oxunmamis == true)
            query = query.Where(x => !x.Oxunub);
        if (tapalanIsciId.HasValue)
            query = query.Where(x => x.TapalanIsciId == tapalanIsciId);

        var mails = await query
            .OrderByDescending(x => x.AlinmaTarixi)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return mails.Select(m => new GelenMailListDto
        {
            Id = m.Id,
            KimdenAd = m.KimdenAd,
            KimdenEmail = m.KimdenEmail,
            Movzu = m.Movzu,
            AlinmaTarixi = m.AlinmaTarixi,
            Oxunub = m.Oxunub,
            AIXulase = m.AIXulase,
            QosmaCount = m.Qosmalar.Count(q => !q.Silinib),
            TapalanIsciAdSoyad = m.TapalanIsci != null ? $"{m.TapalanIsci.Ad} {m.TapalanIsci.Soyad}" : null,
            CavabVerildi = m.CavabVerildi,
            SenedId = m.SenedId
        }).ToList();
    }

    public async Task<GelenMailDetailDto?> GetDetailAsync(int id)
    {
        var m = await _uow.Repository<GelenMail>().Query()
            .AsNoTracking()
            .Where(x => x.Id == id && !x.Silinib)
            .Include(x => x.TapalanIsci)
            .Include(x => x.Qosmalar)
            .FirstOrDefaultAsync();

        if (m == null) return null;

        return new GelenMailDetailDto
        {
            Id = m.Id,
            MessageId = m.MessageId,
            KimdenAd = m.KimdenAd,
            KimdenEmail = m.KimdenEmail,
            Movzu = m.Movzu,
            MetinHtml = m.MetinHtml,
            MetinDuz = m.MetinDuz,
            AlinmaTarixi = m.AlinmaTarixi,
            Oxunub = m.Oxunub,
            AIXulase = m.AIXulase,
            AITahlilTarixi = m.AITahlilTarixi,
            CavabVerildi = m.CavabVerildi,
            SenedId = m.SenedId,
            TapalanIsciId = m.TapalanIsciId,
            TapalanIsciAdSoyad = m.TapalanIsci != null ? $"{m.TapalanIsci.Ad} {m.TapalanIsci.Soyad}" : null,
            TapalanQeyd = m.TapalanQeyd,
            TapalanTarix = m.TapalanTarix,
            Qosmalar = m.Qosmalar.Where(q => !q.Silinib).Select(q => new GelenMailQosmaDto
            {
                Id = q.Id,
                FaylAdi = q.FaylAdi,
                ContentType = q.ContentType,
                OlcuBayt = q.OlcuBayt,
                CixarilmisMetin = q.CixarilmisMetin
            }).ToList()
        };
    }

    public async Task OxunduIsareEtAsync(int id)
    {
        var mail = await _uow.Repository<GelenMail>().Query()
            .Where(x => x.Id == id && !x.Silinib)
            .FirstOrDefaultAsync();

        if (mail != null && !mail.Oxunub)
        {
            mail.Oxunub = true;
            mail.OxunmaTarixi = DateTime.Now;
            await _uow.YaddaSaxlaAsync();
        }
    }

    public async Task<bool> TapaAsync(int mailId, int isciId, string? qeyd, int rehberUserId)
    {
        var mail = await _uow.Repository<GelenMail>().Query()
            .Where(x => x.Id == mailId && !x.Silinib)
            .FirstOrDefaultAsync();

        if (mail == null) return false;

        mail.TapalanIsciId = isciId;
        mail.TapalanIsciTarafindan = rehberUserId;
        mail.TapalanTarix = DateTime.Now;
        mail.TapalanQeyd = qeyd;
        await _uow.YaddaSaxlaAsync();
        return true;
    }

    public async Task<int?> SenedeCevir(int mailId, int qosmaId, int yaradanUserId, string saxlamaKlasoru)
    {
        var mail = await _uow.Repository<GelenMail>().Query()
            .Where(x => x.Id == mailId && !x.Silinib)
            .Include(x => x.Qosmalar)
            .FirstOrDefaultAsync();

        if (mail == null) return null;
        var qosma = mail.Qosmalar.FirstOrDefault(q => q.Id == qosmaId && !q.Silinib);
        if (qosma == null || !File.Exists(qosma.FaylYolu)) return null;

        // Create Sened record pointing to source file
        var sened = new Sened
        {
            Basliq = $"Mail qoşması: {qosma.FaylAdi}",
            AcarSoz = mail.Movzu,
            SenedTarixi = mail.AlinmaTarixi,
            ReferenceType = "GelenMail",
            ReferenceId = mail.Id,
            YaradilmaTarixi = DateTime.Now,
            YaradanIcraciId = yaradanUserId,
            // DepartmentId and SenedNovuId must be supplied by caller or set defaults — use 1 as fallback
            DepartmentId = 1,
            SenedNovuId = 1
        };

        await _uow.Repository<Sened>().YaratAsync(sened);
        await _uow.YaddaSaxlaAsync();

        mail.SenedId = sened.Id;
        await _uow.YaddaSaxlaAsync();

        return sened.Id;
    }

    public async Task<int> GetOxunmamisSayAsync()
    {
        return await _uow.Repository<GelenMail>().Query()
            .AsNoTracking()
            .CountAsync(x => !x.Silinib && !x.Oxunub);
    }

    public async Task SaveAIXulaseAsync(int id, string xulase)
    {
        var mail = await _uow.Repository<GelenMail>().Query()
            .Where(x => x.Id == id && !x.Silinib)
            .FirstOrDefaultAsync();
        if (mail == null) return;
        mail.AIXulase = xulase;
        mail.AITahlilTarixi = DateTime.Now;
        await _uow.YaddaSaxlaAsync();
    }
}
