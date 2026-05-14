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
        IQueryable<GelenMail> query = _uow.Repository<GelenMail>().Query()
            .AsNoTracking()
            .Where(x => !x.Silinib)
            .Include(x => x.TapalanIsci)
            .Include(x => x.TapalanIsciler).ThenInclude(ti => ti.Isci)
            .Include(x => x.Qosmalar);

        if (oxunmamis == true)
            query = query.Where(x => !x.Oxunub);
        if (tapalanIsciId.HasValue)
            query = query.Where(x => x.TapalanIsciler.Any(ti => ti.IsciId == tapalanIsciId));

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
            SenedId = m.SenedId,
            DedlaynTarix = m.DedlaynTarix,
            DedlaynNov = m.DedlaynNov,
            DedlaynQeyd = m.DedlaynQeyd
        }).ToList();
    }

    public async Task<GelenMailDetailDto?> GetDetailAsync(int id)
    {
        var m = await _uow.Repository<GelenMail>().Query()
            .AsNoTracking()
            .Where(x => x.Id == id && !x.Silinib)
            .Include(x => x.TapalanIsci)
            .Include(x => x.TapalanIsciler).ThenInclude(ti => ti.Isci)
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
            TapalanIsciler = m.TapalanIsciler.Select(ti => new GelenMailIsciDto
            {
                IsciId = ti.IsciId,
                AdSoyad = ti.Isci != null ? $"{ti.Isci.Ad} {ti.Isci.Soyad}" : "",
                TapalanTarix = ti.TapalanTarix,
                Qeyd = ti.Qeyd
            }).ToList(),
            DedlaynTarix = m.DedlaynTarix,
            DedlaynNov = m.DedlaynNov,
            DedlaynQeyd = m.DedlaynQeyd,
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

    public async Task<bool> TapaAsync(int mailId, List<int> isciIds, string? qeyd, int rehberUserId)
    {
        var mail = await _uow.Repository<GelenMail>().Query()
            .Where(x => x.Id == mailId && !x.Silinib)
            .Include(x => x.TapalanIsciler)
            .FirstOrDefaultAsync();

        if (mail == null || isciIds.Count == 0) return false;

        // Köhnə tapşırmaları sil
        foreach (var old in mail.TapalanIsciler.ToList())
            old.Silinib = true;

        // Yeni tapşırmalar yarat
        var now = DateTime.Now;
        foreach (var isciId in isciIds)
        {
            mail.TapalanIsciler.Add(new GelenMailIsci
            {
                IsciId = isciId,
                TapalanIsciTarafindan = rehberUserId,
                TapalanTarix = now,
                Qeyd = qeyd,
                YaradilmaTarixi = now
            });
        }

        // Əsas işçi — geriyəuyğunluq
        mail.TapalanIsciId = isciIds.First();
        mail.TapalanIsciTarafindan = rehberUserId;
        mail.TapalanTarix = now;
        mail.TapalanQeyd = qeyd;

        await _uow.YaddaSaxlaAsync();
        return true;
    }

    public async Task<int?> SenedeCevir(int mailId, int qosmaId, int yaradanUserId, int departmentId)
    {
        var mail = await _uow.Repository<GelenMail>().Query()
            .Where(x => x.Id == mailId && !x.Silinib)
            .Include(x => x.Qosmalar)
            .FirstOrDefaultAsync();

        if (mail == null) return null;
        // Duplikat: artıq sənəd dövriyyəsindədirsə əlavə etmə
        if (mail.SenedId.HasValue) return null;

        var qosma = mail.Qosmalar.FirstOrDefault(q => q.Id == qosmaId && !q.Silinib);
        if (qosma == null || !File.Exists(qosma.FaylYolu)) return null;

        var sened = new Sened
        {
            Basliq = $"Mail qoşması: {qosma.FaylAdi}",
            AcarSoz = mail.Movzu,
            SenedTarixi = mail.AlinmaTarixi,
            ReferenceType = "GelenMail",
            ReferenceId = mail.Id,
            YaradilmaTarixi = DateTime.Now,
            YaradanIcraciId = yaradanUserId,
            DepartmentId = departmentId > 0 ? departmentId : 1,
            SenedNovuId = 1
        };

        await _uow.Repository<Sened>().YaratAsync(sened);
        await _uow.YaddaSaxlaAsync();

        // SenedFayl: faylı sənəd dövriyyəsinə qoş
        var fayl = new SenedFayl
        {
            SenedId     = sened.Id,
            VersiyaNo   = 1,
            OriginalAd  = qosma.FaylAdi,
            StoredAd    = Path.GetFileName(qosma.FaylYolu),
            ContentType = qosma.ContentType,
            OlcuBytes   = qosma.OlcuBayt,
            Sha256      = ComputeSha256(qosma.FaylYolu),
            Yol         = qosma.FaylYolu,
            AktivVersiya    = true,
            StorageProvider = "Local",
            YaradilmaTarixi = DateTime.Now
        };
        await _uow.Repository<SenedFayl>().YaratAsync(fayl);

        mail.SenedId = sened.Id;
        await _uow.YaddaSaxlaAsync();

        return sened.Id;
    }

    private static string ComputeSha256(string path)
    {
        try
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            using var stream = File.OpenRead(path);
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }
        catch { return ""; }
    }

    public async Task<int> GetOxunmamisSayAsync()
    {
        return await _uow.Repository<GelenMail>().Query()
            .AsNoTracking()
            .CountAsync(x => !x.Silinib && !x.Oxunub);
    }

    public async Task SilAsync(int id)
    {
        var mail = await _uow.Repository<GelenMail>().Query()
            .Where(x => x.Id == id && !x.Silinib)
            .FirstOrDefaultAsync();
        if (mail == null) return;
        mail.Silinib = true;
        mail.SilinmeTarixi = DateTime.Now;
        await _uow.YaddaSaxlaAsync();
    }

    public async Task SaveAINeticAsync(int id, AIMailTahlilNetic netic)
    {
        var mail = await _uow.Repository<GelenMail>().Query()
            .Where(x => x.Id == id && !x.Silinib)
            .FirstOrDefaultAsync();
        if (mail == null) return;

        mail.AIXulase = netic.Xulase;
        mail.AITahlilTarixi = DateTime.Now;

        if (netic.DedlaynTarix.HasValue && netic.DedlaynTarix.Value > DateTime.Now)
        {
            mail.DedlaynTarix = netic.DedlaynTarix;
            mail.DedlaynNov = netic.DedlaynNov;
            mail.DedlaynQeyd = netic.DedlaynQeyd;
        }

        await _uow.YaddaSaxlaAsync();
    }
}
