using AutoMapper;
using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.Mezuniyyet;
using FinNex.Application.Interfaces;
using FinNex.Application.Interfaces.Communication;
using FinNex.Application.Services;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

public class MezuniyyetService : ServiceAsync<Mezuniyyet, MezuniyyetDto, MezuniyyetCreateDto, MezuniyyetUpdateDto>, IMezuniyyetService
{
    private readonly IEvezediciTesdiqService _evezediciTesdiqService;
    public MezuniyyetService(IUnitOfWork unitOfWork, IMapper mapper,IEvezediciTesdiqService evezediciTesdiqService) : base(unitOfWork, mapper)
    {
        _evezediciTesdiqService = evezediciTesdiqService;
    }

    
    public async Task<Result<IList<MezuniyyetListDto>>> GetListAsync()
    {
        var entities = await _unitOfWork.Repository<Mezuniyyet>()
            .HamisiniGetirAsync(include: x => x.Include(i => i.Isci), izlemeden: true);

        var data = _mapper.Map<IList<MezuniyyetListDto>>(entities);
        return Result<IList<MezuniyyetListDto>>.Ok(data);
    }

    public override async Task<Result<MezuniyyetDto>> YaratAsync(MezuniyyetCreateDto dto)
    {
        using (var transaction = await _unitOfWork.BeginTransactionAsync())
        {
            try
            {
                // 1. İş günlərini hesabla
                int isGunu = await HesablaIsGunuAsync(dto.BaslamaTarixi, dto.BitmeTarixi);

                // 2. Balansı növə görə yoxla (Ezamiyyət limitsizdir)
                if (dto.Nov != MezuniyyetNovu.Ezamiyyet)
                {
                    var balans = await _unitOfWork.Repository<MezuniyyetBalans>()
                        .GetirAsync(x => x.IsciId == dto.IsciId && x.Il == dto.BaslamaTarixi.Year && x.Nov == dto.Nov);

                    if (balans == null || balans.QaliqGun < isGunu)
                        return Result<MezuniyyetDto>.Fail($"Kifayət qədər {dto.Nov} məzuniyyət balansınız yoxdur.");
                }

                // 3. İşçinin aktiv departamentini tap
                var teyinat = await _unitOfWork.Repository<IsciTeyinat>()
                    .GetirAsync(x => x.IsciId == dto.IsciId && x.Aktivdir);

                // 4. İşçinin roluna görə status müəyyən et
                MezuniyyetStatus ilkinStatus;

                if (dto.MuracietSahibiRehberdirmi)
                {
                    // Rəhbər birbaşa HR-a gedir
                    ilkinStatus = MezuniyyetStatus.HrTesdiqinde;
                }
                else if (dto.MuracietSahibiSobeReisidirmi)
                {
                    // Şöbə rəisi öz addımını keçir, rəhbərə gedir
                    ilkinStatus = MezuniyyetStatus.RehberTesdiqinde;
                }
                else
                {
                    // Adi işçi — departamentdə şöbə rəisi varsa ona, yoxsa rəhbərə
                    var sobeReisiVar = teyinat != null && await _unitOfWork.Repository<IsciStrukturRolu>()
                        .MovcuddurmuAsync(x =>
                            x.DepartamentId == teyinat.DepartamentId &&
                            x.RolTipi == StrukturRolTipi.SobeReisi &&
                            x.IsciId != dto.IsciId &&
                            x.Aktivdir);

                    ilkinStatus = sobeReisiVar
                        ? MezuniyyetStatus.SobeReisiTesdiqinde
                        : MezuniyyetStatus.RehberTesdiqinde;
                }

                // 6. Entity-ni yarat
                var entity = _mapper.Map<Mezuniyyet>(dto);
                entity.IsGunlerininSayi = isGunu;
                entity.Status = ilkinStatus;

                await _unitOfWork.Repository<Mezuniyyet>().YaratAsync(entity);
                await _unitOfWork.YaddaSaxlaAsync();
                // Əvəzedici varsa sorğu yarat
                if (dto.EvezEdenIsciId.HasValue)
                {
                    await _evezediciTesdiqService.YaratAsync(entity.Id, dto.EvezEdenIsciId.Value);
                }
                await transaction.CommitAsync();

                return Result<MezuniyyetDto>.Ok(_mapper.Map<MezuniyyetDto>(entity), "Müraciət uğurla göndərildi.");
            }
            catch
            {
                await transaction.RollbackAsync();
                return Result<MezuniyyetDto>.Fail("Xəta baş verdi.");
            }
        }
    }

    // Təsdiq Metodları (Workflow məntiqi)
    public async Task<Result> SobeReisiTesdiqAsync(int id, bool status, string? qeyd, int sobeReisiId)
    {
        var m = await _unitOfWork.Repository<Mezuniyyet>()
            .GetirAsync(x => x.Id == id);
        if (m == null) return Result.Fail("Müraciət tapılmadı.");

        m.SobeReisiTesdiq = status;
        m.SobeReisiId = sobeReisiId;
        m.SobeReisiTesdiqTarixi = DateTime.Now;
        m.Status = status
            ? MezuniyyetStatus.RehberTesdiqinde
            : MezuniyyetStatus.ImtinaEdildi;

        if (!status) m.ImtinaSebebi = qeyd;

        await _unitOfWork.Repository<Mezuniyyet>().YenileAsync(m);
        await _unitOfWork.YaddaSaxlaAsync();
        return Result.Ok("Şöbə rəisi qərarı qeydə alındı.");
    }

    // --- Yardımçı Alqoritm: İş günlərini bayramlara görə hesablayır ---
    private async Task<int> HesablaIsGunuAsync(DateTime baslangic, DateTime bitis)
    {
        var bayramlar = await _unitOfWork.Repository<BayramGunu>()
            .HamisiniGetirAsync(x => x.Tarix >= baslangic && x.Tarix <= bitis);

        int count = 0;
        for (var date = baslangic; date <= bitis; date = date.AddDays(1))
        {
            // Şənbə (6) və Bazar (0) günləri deyilsə VƏ bayram günləri siyahısında yoxdursa
            if (date.DayOfWeek != DayOfWeek.Saturday &&
                date.DayOfWeek != DayOfWeek.Sunday &&
                !bayramlar.Any(b => b.Tarix.Date == date.Date))
            {
                count++;
            }
        }
        return count;
    }

    public async Task<Result> RehberTesdiqAsync(int id, bool status, string? qeyd, int rehberId)
    {
        var m = await _unitOfWork.Repository<Mezuniyyet>()
            .GetirAsync(x => x.Id == id); // ← izlemeden olmadan, tracking ilə

        if (m == null) return Result.Fail("Müraciət tapılmadı.");

        m.RehberTesdiq = status;
        m.RehberId = rehberId;
        m.RehberTesdiqTarixi = DateTime.Now;
        m.Status = status
            ? MezuniyyetStatus.HrTesdiqinde
            : MezuniyyetStatus.ImtinaEdildi;

        if (!status) m.ImtinaSebebi = qeyd;

        await _unitOfWork.Repository<Mezuniyyet>().YenileAsync(m); // ← explicit update
        await _unitOfWork.YaddaSaxlaAsync();
        return Result.Ok("Rəhbər qərarı qeydə alındı.");
    }
    // HrTesdiqAsync metodu
    public async Task<Result> HrTesdiqAsync(int id, bool status, string? qeyd, int hrId)
    {
        var m = await _unitOfWork.Repository<Mezuniyyet>()
            .GetirAsync(x => x.Id == id);
        if (m == null) return Result.Fail("Müraciət tapılmadı.");

        m.HrTesdiq = status;
        m.HrId = hrId;
        m.HrTesdiqTarixi = DateTime.Now;
        m.Status = status
            ? MezuniyyetStatus.Tesdiqlenib
            : MezuniyyetStatus.ImtinaEdildi;

        if (!status) m.ImtinaSebebi = qeyd;

        await _unitOfWork.Repository<Mezuniyyet>().YenileAsync(m);

        // Təsdiqlənibsə: balansı yenilə + davamiyyətdə icazəli qeydlər yarat
        if (status)
        {
            // Balansı yenilə
            var balans = await _unitOfWork.Repository<MezuniyyetBalans>()
                .GetirAsync(x => x.IsciId == m.IsciId && x.Il == m.BaslamaTarixi.Year && x.Nov == m.Nov);

            if (balans != null)
            {
                balans.IstifadeOlunanGun += m.IsGunlerininSayi;
                await _unitOfWork.Repository<MezuniyyetBalans>().YenileAsync(balans);
            }

            // Davamiyyətdə İcazəli qeydlər yarat (hər iş günü üçün)
            var bayramlar = await _unitOfWork.Repository<BayramGunu>()
                .HamisiniGetirAsync(x => x.Tarix >= m.BaslamaTarixi && x.Tarix <= m.BitmeTarixi);

            for (var gun = m.BaslamaTarixi; gun <= m.BitmeTarixi; gun = gun.AddDays(1))
            {
                if (gun.DayOfWeek == DayOfWeek.Saturday || gun.DayOfWeek == DayOfWeek.Sunday)
                    continue;
                if (bayramlar.Any(b => b.Tarix.Date == gun.Date))
                    continue;

                // Artıq qeyd varsa yaratma
                var movcud = await _unitOfWork.Repository<Davamiyyet>()
                    .MovcuddurmuAsync(x => x.IsciId == m.IsciId && x.Tarix.Date == gun.Date);
                if (movcud) continue;

                var dav = new Davamiyyet
                {
                    IsciId = m.IsciId,
                    Tarix = gun,
                    Status = DavamiyyetStatus.Icazeli
                };
                await _unitOfWork.Repository<Davamiyyet>().YaratAsync(dav);
            }
        }

        await _unitOfWork.YaddaSaxlaAsync();
        return Result.Ok("HR qərarı qeydə alındı.");
    }
    // ============================================================
    // Bu faylı MezuniyyetService.cs-ə əlavə edin:
    // HrTesdiqAsync metodundan SONRA bu iki metodu əlavə edin.
    // ============================================================

    public async Task<Result<IList<MezuniyyetListDto>>> GetIsciMezuniyyetleriAsync(int isciId)
    {
        try
        {
            var entities = await _unitOfWork.Repository<Mezuniyyet>()
            .HamisiniGetirAsync(
                predicate: x => x.IsciId == isciId,
                include: q => q
    .Include(m => m.Isci)
        .ThenInclude(i => i.IsciTeyinatlari)
            .ThenInclude(t => t.Departament)
    .Include(m => m.Isci)
        .ThenInclude(i => i.IsciTeyinatlari)
            .ThenInclude(t => t.Vezife)
    .Include(m => m.EvezEdenIsci),
                izlemeden: true);

            var dtos = entities
                .OrderByDescending(x => x.BaslamaTarixi)
                .Select(m => new MezuniyyetListDto
                {
                    Id = m.Id,
                    IsciAdSoyad = m.Isci.TamAd,
                    SobeAdi = m.Isci.IsciTeyinatlari
                    .Where(t => t.Aktivdir)
                    .Select(t => t.Departament.Ad)
                    .FirstOrDefault() ?? "-",
                    VezifeAdi = m.Isci.IsciTeyinatlari
                    .Where(t => t.Aktivdir)
                    .Select(t => t.Vezife.Ad)
                    .FirstOrDefault() ?? "-",
                    EvezEdenIsciAdSoyad = m.EvezEdenIsci?.TamAd,
                    Nov = m.Nov,
                    Status = m.Status,
                    BaslamaTarixi = m.BaslamaTarixi,
                    BitmeTarixi = m.BitmeTarixi,
                    IsGunlerininSayi = m.IsGunlerininSayi,
                }).ToList();

            return Result<IList<MezuniyyetListDto>>.Ok(dtos);
        }
        catch (Exception ex)
        {
            return Result<IList<MezuniyyetListDto>>.Fail($"Məzuniyyətlər gətirilmədi: {ex.Message}");
        }
    }

    public async Task<Result<IList<MezuniyyetListDto>>> GetGozlemededeAsync()
    {
        try
        {
            var entities = await _unitOfWork.Repository<Mezuniyyet>()
                .HamisiniGetirAsync(
                    predicate: x => x.Status == MezuniyyetStatus.Gozlemede,
                    include: q => q
    .Include(m => m.Isci)
        .ThenInclude(i => i.IsciTeyinatlari)
            .ThenInclude(t => t.Departament)
    .Include(m => m.Isci)
        .ThenInclude(i => i.IsciTeyinatlari)
            .ThenInclude(t => t.Vezife)
    .Include(m => m.EvezEdenIsci),
                    izlemeden: true);

            var dtos = entities
                .OrderByDescending(x => x.BaslamaTarixi)
                .Select(m => new MezuniyyetListDto
                {
                    Id = m.Id,
                    IsciAdSoyad = m.Isci.TamAd,
                    SobeAdi = m.Isci.IsciTeyinatlari
                        .Where(t => t.Aktivdir)
                        .Select(t => t.Departament.Ad)
                        .FirstOrDefault() ?? "-",
                    VezifeAdi = m.Isci.IsciTeyinatlari
                        .Where(t => t.Aktivdir)
                        .Select(t => t.Vezife.Ad)
                        .FirstOrDefault() ?? "-",
                    EvezEdenIsciAdSoyad = m.EvezEdenIsci?.TamAd,
                    Nov = m.Nov,
                    Status = m.Status,
                    BaslamaTarixi = m.BaslamaTarixi,
                    BitmeTarixi = m.BitmeTarixi,
                    IsGunlerininSayi = m.IsGunlerininSayi,
                }).ToList();

            return Result<IList<MezuniyyetListDto>>.Ok(dtos);
        }
        catch (Exception ex)
        {
            return Result<IList<MezuniyyetListDto>>.Fail($"Gözləmədə məzuniyyətlər gətirilmədi: {ex.Message}");
        }
    }

    public async Task<Result<IList<MezuniyyetListDto>>> GetRehberTesdiqindeAsync()
    {
        try
        {
            var entities = await _unitOfWork.Repository<Mezuniyyet>()
                .HamisiniGetirAsync(
                    predicate: x => x.Status == MezuniyyetStatus.RehberTesdiqinde,
                    include: q => q
    .Include(m => m.Isci)
        .ThenInclude(i => i.IsciTeyinatlari)
            .ThenInclude(t => t.Departament)   // ← əlavə et
    .Include(m => m.Isci)
        .ThenInclude(i => i.IsciTeyinatlari)
            .ThenInclude(t => t.Vezife)         // ← əlavə et
    .Include(m => m.EvezEdenIsci),
                    izlemeden: true);

            var dtos = entities
                .OrderByDescending(x => x.BaslamaTarixi)
                .Select(m => new MezuniyyetListDto
                {
                    Id = m.Id,
                    IsciAdSoyad = m.Isci.TamAd,
                    SobeAdi = m.Isci.IsciTeyinatlari
                        .Where(t => t.Aktivdir)
                        .Select(t => t.Departament.Ad)
                        .FirstOrDefault() ?? "-",
                    VezifeAdi = m.Isci.IsciTeyinatlari
                        .Where(t => t.Aktivdir)
                        .Select(t => t.Vezife.Ad)
                        .FirstOrDefault() ?? "-",
                    EvezEdenIsciAdSoyad = m.EvezEdenIsci?.TamAd,
                    Nov = m.Nov,
                    Status = m.Status,
                    BaslamaTarixi = m.BaslamaTarixi,
                    BitmeTarixi = m.BitmeTarixi,
                    IsGunlerininSayi = m.IsGunlerininSayi,
                }).ToList();

            return Result<IList<MezuniyyetListDto>>.Ok(dtos);
        }
        catch (Exception ex)
        {
            return Result<IList<MezuniyyetListDto>>.Fail($"Rəhbər təsdiqindəki məzuniyyətlər gətirilmədi: {ex.Message}");
        }
    }

    public async Task<Result<IList<MezuniyyetListDto>>> GetHrTesdiqindeAsync()
    {
        try
        {
            var entities = await _unitOfWork.Repository<Mezuniyyet>()
                .HamisiniGetirAsync(
                    predicate: x => x.Status == MezuniyyetStatus.HrTesdiqinde,
                    include: q => q
    .Include(m => m.Isci)
        .ThenInclude(i => i.IsciTeyinatlari)
            .ThenInclude(t => t.Departament)
    .Include(m => m.Isci)
        .ThenInclude(i => i.IsciTeyinatlari)
            .ThenInclude(t => t.Vezife)
    .Include(m => m.EvezEdenIsci),
                    izlemeden: true);

            var dtos = entities
                .OrderByDescending(x => x.BaslamaTarixi)
                .Select(m => new MezuniyyetListDto
                {
                    Id = m.Id,
                    IsciAdSoyad = m.Isci.TamAd,
                    SobeAdi = m.Isci.IsciTeyinatlari
                        .Where(t => t.Aktivdir)
                        .Select(t => t.Departament.Ad)
                        .FirstOrDefault() ?? "-",
                    VezifeAdi = m.Isci.IsciTeyinatlari
                        .Where(t => t.Aktivdir)
                        .Select(t => t.Vezife.Ad)
                        .FirstOrDefault() ?? "-",
                    EvezEdenIsciAdSoyad = m.EvezEdenIsci?.TamAd,
                    Nov = m.Nov,
                    Status = m.Status,
                    BaslamaTarixi = m.BaslamaTarixi,
                    BitmeTarixi = m.BitmeTarixi,
                    IsGunlerininSayi = m.IsGunlerininSayi,
                }).ToList();

            return Result<IList<MezuniyyetListDto>>.Ok(dtos);
        }
        catch (Exception ex)
        {
            return Result<IList<MezuniyyetListDto>>.Fail($"HR təsdiqindəki məzuniyyətlər gətirilmədi: {ex.Message}");
        }
    }

    public async Task<Result> LegvEtAsync(int id, int isciId)
    {
        var m = await _unitOfWork.Repository<Mezuniyyet>().IdIleGetirAsync(id);

        if (m == null)
            return Result.Fail("Müraciət tapılmadı.");

        if (m.IsciId != isciId)
            return Result.Fail("Bu müraciət sizə aid deyil.");

        if (m.Status != MezuniyyetStatus.Gozlemede &&
            m.Status != MezuniyyetStatus.SobeReisiTesdiqinde &&
            m.Status != MezuniyyetStatus.RehberTesdiqinde &&
            m.Status != MezuniyyetStatus.Tesdiqlenib)
            return Result.Fail("Bu statusda ləğv etmək mümkün deyil.");

        // Təsdiqlənmiş məzuniyyəti ləğv edəndə balansı geri qaytar
        if (m.Status == MezuniyyetStatus.Tesdiqlenib)
        {
            var balans = await _unitOfWork.Repository<MezuniyyetBalans>()
                .GetirAsync(x => x.IsciId == m.IsciId && x.Il == m.BaslamaTarixi.Year && x.Nov == m.Nov);

            if (balans != null)
            {
                balans.IstifadeOlunanGun = Math.Max(0, balans.IstifadeOlunanGun - m.IsGunlerininSayi);
                await _unitOfWork.Repository<MezuniyyetBalans>().YenileAsync(balans);
            }

            // Davamiyyətdəki İcazəli qeydləri sil
            var davQeydleri = await _unitOfWork.Repository<Davamiyyet>()
                .HamisiniGetirAsync(x =>
                    x.IsciId == m.IsciId &&
                    x.Status == DavamiyyetStatus.Icazeli &&
                    x.Tarix >= m.BaslamaTarixi &&
                    x.Tarix <= m.BitmeTarixi);

            foreach (var dav in davQeydleri)
                await _unitOfWork.Repository<Davamiyyet>().YumshakSilAsync(dav.Id);
        }

        await _unitOfWork.Repository<Mezuniyyet>().YumshakSilAsync(id);
        await _unitOfWork.YaddaSaxlaAsync();

        return Result.Ok("Müraciət ləğv edildi.");
    }
    // İşçinin departament ID-sinə görə SobeReisiTesdiqinde statuslu müraciətlər
    public async Task<Result<IList<MezuniyyetListDto>>> GetSobeyeGoreMezuniyyetlerAsync(int departamentId, int sobeReisiIsciId)
    {
        try
        {
            var entities = await _unitOfWork.Repository<Mezuniyyet>()
                .HamisiniGetirAsync(
                    predicate: x => x.Status == MezuniyyetStatus.SobeReisiTesdiqinde &&
                    x.IsciId != sobeReisiIsciId &&  // ← özünü görməsin
                    x.Isci.IsciTeyinatlari
                        .Any(t => t.Aktivdir && t.DepartamentId == departamentId),
                    include: q => q
                        .Include(m => m.Isci)
                            .ThenInclude(i => i.IsciTeyinatlari)
                                .ThenInclude(t => t.Departament)
                        .Include(m => m.Isci)
                            .ThenInclude(i => i.IsciTeyinatlari)
                                .ThenInclude(t => t.Vezife)
                        .Include(m => m.EvezEdenIsci),
                    izlemeden: true);

            var dtos = entities
                .OrderByDescending(x => x.BaslamaTarixi)
                .Select(m => new MezuniyyetListDto
                {
                    Id = m.Id,
                    IsciAdSoyad = m.Isci.TamAd,
                    SobeAdi = m.Isci.IsciTeyinatlari
                        .Where(t => t.Aktivdir)
                        .Select(t => t.Departament.Ad)
                        .FirstOrDefault() ?? "-",
                    VezifeAdi = m.Isci.IsciTeyinatlari
                        .Where(t => t.Aktivdir)
                        .Select(t => t.Vezife.Ad)
                        .FirstOrDefault() ?? "-",
                    EvezEdenIsciAdSoyad = m.EvezEdenIsci?.TamAd,
                    Nov = m.Nov,
                    Status = m.Status,
                    BaslamaTarixi = m.BaslamaTarixi,
                    BitmeTarixi = m.BitmeTarixi,
                    IsGunlerininSayi = m.IsGunlerininSayi,
                }).ToList();

            return Result<IList<MezuniyyetListDto>>.Ok(dtos);
        }
        catch (Exception ex)
        {
            return Result<IList<MezuniyyetListDto>>.Fail($"Xəta: {ex.Message}");
        }
    }
    public override async Task<Result<MezuniyyetDto>> IdIleGetirAsync(int id)
    {
        var entity = await _unitOfWork.Repository<Mezuniyyet>()
            .GetirAsync(
                predicate: x => x.Id == id,
                include: q => q
                    .Include(m => m.Isci)
                        .ThenInclude(i => i.IsciTeyinatlari)
                            .ThenInclude(t => t.Departament)
                    .Include(m => m.Isci)
                        .ThenInclude(i => i.IsciTeyinatlari)
                            .ThenInclude(t => t.Vezife)
                    .Include(m => m.EvezEdenIsci),
                izlemeden: true);

        if (entity == null)
            return Result<MezuniyyetDto>.Fail("Tapılmadı.");

        var dto = new MezuniyyetDto
        {
            Id = entity.Id,
            IsciId = entity.IsciId,
            IsciAdSoyad = entity.Isci.TamAd,
            SobeAdi = entity.Isci.IsciTeyinatlari
                .Where(t => t.Aktivdir)
                .Select(t => t.Departament.Ad)
                .FirstOrDefault() ?? "-",
            VezifeAdi = entity.Isci.IsciTeyinatlari
                .Where(t => t.Aktivdir)
                .Select(t => t.Vezife.Ad)
                .FirstOrDefault() ?? "-",
            EvezEdenIsciId = entity.EvezEdenIsciId,
            EvezEdenIsciAdSoyad = entity.EvezEdenIsci?.TamAd,
            Nov = entity.Nov,
            Status = entity.Status,
            BaslamaTarixi = entity.BaslamaTarixi,
            BitmeTarixi = entity.BitmeTarixi,
            IsGunlerininSayi = entity.IsGunlerininSayi,
            Qeyd = entity.Qeyd,
            ImtinaSebebi = entity.ImtinaSebebi,
            SobeReisiTesdiq = entity.SobeReisiTesdiq,
            SobeReisiTesdiqTarixi = entity.SobeReisiTesdiqTarixi,
            RehberTesdiq = entity.RehberTesdiq,
            RehberTesdiqTarixi = entity.RehberTesdiqTarixi,
            HrTesdiq = entity.HrTesdiq,
            HrTesdiqTarixi = entity.HrTesdiqTarixi,
        };

        return Result<MezuniyyetDto>.Ok(dto);
    }
    public async Task<Result<IList<MezuniyyetListDto>>> GetFiltrliAsync(
    DateTime? baslaTarixFrom,
    DateTime? baslaTarixTo,
    int? departamentId,
    int? status,
    string? axtaris)
    {
        try
        {
            var entities = await _unitOfWork.Repository<Mezuniyyet>()
                .HamisiniGetirAsync(
                    predicate: x =>
                        (baslaTarixFrom == null || x.BaslamaTarixi >= baslaTarixFrom) &&
                        (baslaTarixTo == null || x.BaslamaTarixi <= baslaTarixTo) &&
                        (status == null || (int)x.Status == status) &&
                        (departamentId == null || x.Isci.IsciTeyinatlari
                            .Any(t => t.Aktivdir && t.DepartamentId == departamentId)) &&
                        (axtaris == null || x.Isci.Ad.Contains(axtaris) ||
                            x.Isci.Soyad.Contains(axtaris)),
                    include: q => q
                        .Include(m => m.Isci)
                            .ThenInclude(i => i.IsciTeyinatlari)
                                .ThenInclude(t => t.Departament)
                        .Include(m => m.Isci)
                            .ThenInclude(i => i.IsciTeyinatlari)
                                .ThenInclude(t => t.Vezife)
                        .Include(m => m.EvezEdenIsci)
                        .Include(m => m.SobeReisiIsci)   // ← navigation property
                        .Include(m => m.RehberIsci)       // ← navigation property
                        .Include(m => m.HrIsci),          // ← navigation property
                    izlemeden: true);

            var dtos = entities
                .OrderByDescending(x => x.BaslamaTarixi)
                .Select(m => new MezuniyyetListDto
                {
                    Id = m.Id,
                    IsciAdSoyad = m.Isci.TamAd,
                    SobeAdi = m.Isci.IsciTeyinatlari
                        .Where(t => t.Aktivdir)
                        .Select(t => t.Departament.Ad)
                        .FirstOrDefault() ?? "-",
                    VezifeAdi = m.Isci.IsciTeyinatlari
                        .Where(t => t.Aktivdir)
                        .Select(t => t.Vezife.Ad)
                        .FirstOrDefault() ?? "-",
                    EvezEdenIsciAdSoyad = m.EvezEdenIsci?.TamAd,
                    Nov = m.Nov,
                    Status = m.Status,
                    BaslamaTarixi = m.BaslamaTarixi,
                    BitmeTarixi = m.BitmeTarixi,
                    IsGunlerininSayi = m.IsGunlerininSayi,
                    SobeReisiAdSoyad = m.SobeReisiIsci?.TamAd,
                    SobeReisiTesdiq = m.SobeReisiTesdiq,
                    SobeReisiTesdiqTarixi = m.SobeReisiTesdiqTarixi,
                    RehberAdSoyad = m.RehberIsci?.TamAd,
                    RehberTesdiq = m.RehberTesdiq,
                    RehberTesdiqTarixi = m.RehberTesdiqTarixi,
                    HrAdSoyad = m.HrIsci?.TamAd,
                    HrTesdiq = m.HrTesdiq,
                    HrTesdiqTarixi = m.HrTesdiqTarixi,
                    ImtinaSebebi = m.ImtinaSebebi,
                    YaradilmaTarixi = m.YaradilmaTarixi,
                }).ToList();

            return Result<IList<MezuniyyetListDto>>.Ok(dtos);
        }
        catch (Exception ex)
        {
            return Result<IList<MezuniyyetListDto>>.Fail($"Xəta: {ex.Message}");
        }
    }
}