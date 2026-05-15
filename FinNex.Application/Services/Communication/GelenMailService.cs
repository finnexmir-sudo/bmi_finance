using FinNex.Application.DTOs.Communication;
using FinNex.Application.Interfaces.Communication;
using FinNex.Domain.Entities.Communication;
using FinNex.Domain.Entities.HR;
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

    public async Task<List<GelenMailListDto>> GetListAsync(bool? oxunmamis = null, int? tapalanIsciId = null, int page = 1, int pageSize = 50, string? axtaris = null, DateTime? tarixden = null, DateTime? tarixe = null, bool? tapshirildi = null, bool? qosmali = null, string? deadlineFilter = null)
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
        if (!string.IsNullOrWhiteSpace(axtaris))
        {
            var q = axtaris.Trim().ToLower();
            query = query.Where(x =>
                x.Movzu.ToLower().Contains(q) ||
                x.KimdenAd.ToLower().Contains(q) ||
                x.KimdenEmail.ToLower().Contains(q));
        }
        if (tarixden.HasValue)
            query = query.Where(x => x.AlinmaTarixi >= tarixden.Value);
        if (tarixe.HasValue)
            query = query.Where(x => x.AlinmaTarixi < tarixe.Value.AddDays(1));
        if (tapshirildi == true)
            query = query.Where(x => x.TapalanIsciler.Any(ti => !ti.Silinib));
        if (tapshirildi == false)
            query = query.Where(x => !x.TapalanIsciler.Any(ti => !ti.Silinib));
        if (qosmali == true)
            query = query.Where(x => x.Qosmalar.Any(q => !q.Silinib));
        if (!string.IsNullOrWhiteSpace(deadlineFilter))
        {
            var today = DateTime.Today;
            query = deadlineFilter switch
            {
                "var"     => query.Where(x => x.DedlaynTarix.HasValue),
                "kecib"   => query.Where(x => x.DedlaynTarix.HasValue && x.DedlaynTarix < today),
                "buhefte" => query.Where(x => x.DedlaynTarix.HasValue && x.DedlaynTarix >= today && x.DedlaynTarix < today.AddDays(7)),
                _         => query
            };
        }

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
            TapalanIsciler = m.TapalanIsciler.Where(ti => !ti.Silinib).Select(ti => new GelenMailIsciDto
            {
                IsciId = ti.IsciId,
                AdSoyad = ti.Isci != null ? $"{ti.Isci.Ad} {ti.Isci.Soyad}" : "",
                TapalanTarix = ti.TapalanTarix,
                Qeyd = ti.Qeyd,
                IcraOlundu = ti.IcraOlundu,
                IcraOlunduTarix = ti.IcraOlunduTarix,
                ImtinaEtdi = ti.ImtinaEtdi,
                ImtinaSebebi = ti.ImtinaSebebi,
                IsciOxudu = ti.IsciOxudu,
                IsciOxuduTarix = ti.IsciOxuduTarix
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

    public async Task<int?> SenedeCevir(int mailId, int qosmaId, int yaradanUserId, int? isciId)
    {
        var mail = await _uow.Repository<GelenMail>().Query()
            .Where(x => x.Id == mailId && !x.Silinib)
            .Include(x => x.Qosmalar)
            .FirstOrDefaultAsync();

        if (mail == null) return null;
        if (mail.SenedId.HasValue) return null;

        var qosma = mail.Qosmalar.FirstOrDefault(q => q.Id == qosmaId && !q.Silinib);
        if (qosma == null || !File.Exists(qosma.FaylYolu)) return null;

        // Şöbə: IsciTeyinat-dan aktiv teyin (UserDepartments deyil)
        int departmentId = 1;
        if (isciId.HasValue)
        {
            var teyinat = await _uow.Repository<IsciTeyinat>().Query()
                .Where(t => t.IsciId == isciId.Value && t.Aktivdir && !t.Silinib)
                .OrderByDescending(t => t.Id)
                .FirstOrDefaultAsync();
            if (teyinat != null)
                departmentId = teyinat.DepartamentId;
        }

        // Sənəd nömrəsi: SenedNovu.Kod-İL-SıraNo
        const int senedNovuId = 1;
        var nov = await _uow.Repository<SenedNovu>().Query()
            .FirstOrDefaultAsync(x => x.Id == senedNovuId);
        var kod = nov?.Kod ?? "MAIL";
        var currentYear = DateTime.Now.Year;
        var existingCount = await _uow.Repository<Sened>().Query()
            .CountAsync(x => x.SenedNovuId == senedNovuId && x.YaradilmaTarixi.Year == currentYear);
        var senedNomresi = $"{kod}-{currentYear}-{(existingCount + 1):D3}";

        var sened = new Sened
        {
            Basliq = $"Mail qoşması: {qosma.FaylAdi}",
            AcarSoz = mail.Movzu,
            SenedTarixi = mail.AlinmaTarixi,
            ReferenceType = "GelenMail",
            ReferenceId = mail.Id,
            YaradilmaTarixi = DateTime.Now,
            YaradanIcraciId = yaradanUserId,
            DepartmentId = departmentId,
            SenedNovuId = senedNovuId,
            SenedNomresi = senedNomresi
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

    public async Task<List<GelenMailTapshiriqDto>> GetMailTapshiriqlariAsync(int isciId)
    {
        var records = await _uow.Repository<GelenMailIsci>().Query()
            .AsNoTracking()
            .Where(x => x.IsciId == isciId && !x.Silinib)
            .Include(x => x.GelenMail)
            .OrderByDescending(x => x.TapalanTarix)
            .ToListAsync();

        return records.Select(x => new GelenMailTapshiriqDto
        {
            MailId          = x.GelenMailId,
            Movzu           = x.GelenMail.Movzu,
            KimdenAd        = x.GelenMail.KimdenAd,
            KimdenEmail     = x.GelenMail.KimdenEmail,
            AlinmaTarixi    = x.GelenMail.AlinmaTarixi,
            TapalanTarix    = x.TapalanTarix,
            Qeyd            = x.Qeyd,
            IcraOlundu      = x.IcraOlundu,
            IcraOlunduTarix = x.IcraOlunduTarix,
            ImtinaEtdi      = x.ImtinaEtdi,
            ImtinaSebebi    = x.ImtinaSebebi
        }).ToList();
    }

    public async Task<bool> IcraEtAsync(int mailId, int isciId)
    {
        var record = await _uow.Repository<GelenMailIsci>().Query()
            .Where(x => x.GelenMailId == mailId && x.IsciId == isciId && !x.Silinib)
            .FirstOrDefaultAsync();
        if (record == null) return false;

        record.IcraOlundu = true;
        record.IcraOlunduTarix = DateTime.Now;
        record.ImtinaEtdi = false;
        record.ImtinaSebebi = null;
        await _uow.YaddaSaxlaAsync();
        return true;
    }

    public async Task<bool> ImtinaEtAsync(int mailId, int isciId, string? sebeb)
    {
        var record = await _uow.Repository<GelenMailIsci>().Query()
            .Where(x => x.GelenMailId == mailId && x.IsciId == isciId && !x.Silinib)
            .FirstOrDefaultAsync();
        if (record == null) return false;

        record.ImtinaEtdi = true;
        record.ImtinaSebebi = sebeb;
        record.IcraOlundu = false;
        record.IcraOlunduTarix = null;
        await _uow.YaddaSaxlaAsync();
        return true;
    }

    public async Task<List<MailTapshirilanDto>> GetRehberTapshirilanMaillerAsync(int rehberUserId)
    {
        var records = await _uow.Repository<GelenMailIsci>().Query()
            .AsNoTracking()
            .Where(x => x.TapalanIsciTarafindan == rehberUserId && !x.Silinib)
            .Include(x => x.GelenMail)
            .Include(x => x.Isci)
            .OrderByDescending(x => x.TapalanTarix)
            .ToListAsync();

        return records
            .GroupBy(x => x.GelenMailId)
            .Select(g =>
            {
                var first = g.First();
                return new MailTapshirilanDto
                {
                    MailId       = g.Key,
                    Movzu        = first.GelenMail.Movzu,
                    KimdenAd     = first.GelenMail.KimdenAd,
                    KimdenEmail  = first.GelenMail.KimdenEmail,
                    AlinmaTarixi = first.GelenMail.AlinmaTarixi,
                    TapalanTarix = first.TapalanTarix,
                    Qeyd         = first.Qeyd,
                    Isciler      = g.Select(x => new MailTapshirilanIsciDto
                    {
                        IsciId          = x.IsciId,
                        AdSoyad         = x.Isci != null ? $"{x.Isci.Ad} {x.Isci.Soyad}" : "",
                        TapalanTarix    = x.TapalanTarix,
                        Qeyd            = x.Qeyd,
                        IcraOlundu      = x.IcraOlundu,
                        IcraOlunduTarix = x.IcraOlunduTarix,
                        ImtinaEtdi      = x.ImtinaEtdi,
                        ImtinaSebebi    = x.ImtinaSebebi,
                        IsciOxudu       = x.IsciOxudu,
                        IsciOxuduTarix  = x.IsciOxuduTarix
                    }).ToList()
                };
            })
            .OrderByDescending(x => x.TapalanTarix)
            .ToList();
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

    public async Task CavabVerildiIsareEtAsync(int mailId)
    {
        var mail = await _uow.Repository<GelenMail>().Query()
            .Where(x => x.Id == mailId && !x.Silinib)
            .FirstOrDefaultAsync();
        if (mail == null) return;
        mail.CavabVerildi = true;
        await _uow.YaddaSaxlaAsync();
    }

    public async Task IsciOxuduIsareEtAsync(int mailId, int isciId)
    {
        var record = await _uow.Repository<GelenMailIsci>().Query()
            .Where(x => x.GelenMailId == mailId && x.IsciId == isciId && !x.Silinib)
            .FirstOrDefaultAsync();
        if (record == null || record.IsciOxudu) return;

        record.IsciOxudu = true;
        record.IsciOxuduTarix = DateTime.Now;
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
