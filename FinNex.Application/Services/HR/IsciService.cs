using AutoMapper;
using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.Isci;
using FinNex.Application.DTOs.HR.Mezuniyyet;
using FinNex.Application.Services;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq;

public class IsciService : ServiceAsync<Isci, IsciListDto, IsciCreateDto, IsciUpdateDto>, IIsciService
{
    public IsciService(IUnitOfWork unitOfWork, IMapper mapper) : base(unitOfWork, mapper) { }

    public override async Task<Result<IList<IsciListDto>>> HamisiniGetirAsync()
    {
        try
        {
            var entities = await _unitOfWork.Repository<Isci>()
                .HamisiniGetirAsync(
                    izlemeden: true,
                    include: x => x
                        .Include(i => i.IsciTeyinatlari).ThenInclude(t => t.Departament)
                        .Include(i => i.IsciTeyinatlari).ThenInclude(t => t.Vezife)
                        .Include(i => i.Maliye)
                );

            // İşçi Sıralaması səhifəsində təyin edilmiş sıra ilə düz
            entities = entities
                .OrderBy(x => x.Sira)
                .ThenBy(x => x.Ad)
                .ThenBy(x => x.Soyad)
                .ToList();

            var data = _mapper.Map<IList<IsciListDto>>(entities);
            return Result<IList<IsciListDto>>.Ok(data);
        }
        catch (Exception)
        {
            return Result<IList<IsciListDto>>.Fail("İşçi siyahısı gətirilərkən xəta baş verdi.");
        }
    }

    /// <summary>
    /// Predikat və include ilə işçi siyahısı — Sira ilə düzülmüş.
    /// Bu overload-u override edirik ki, bütün filtirlənmiş çağırışlar da
    /// "İşçi Sıralaması" qaydasına əməl etsin.
    /// </summary>
    public override async Task<Result<IList<IsciListDto>>> HamisiniGetirAsync(
        System.Linq.Expressions.Expression<Func<Isci, bool>>? predicate = null,
        Func<IQueryable<Isci>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Isci, object>>? include = null,
        bool izlemeden = true)
    {
        try
        {
            var entities = await _unitOfWork.Repository<Isci>()
                .HamisiniGetirAsync(predicate, include, izlemeden);

            entities = entities
                .OrderBy(x => x.Sira)
                .ThenBy(x => x.Ad)
                .ThenBy(x => x.Soyad)
                .ToList();

            var data = _mapper.Map<IList<IsciListDto>>(entities);
            return Result<IList<IsciListDto>>.Ok(data);
        }
        catch (Exception)
        {
            return Result<IList<IsciListDto>>.Fail("İşçi siyahısı gətirilərkən xəta baş verdi.");
        }
    }

    public override async Task<Result<IsciListDto>> YaratAsync(IsciCreateDto dto)
    {
        try
        {
            var isci = _mapper.Map<Isci>(dto);
            await _unitOfWork.Repository<Isci>().YaratAsync(isci);
            await _unitOfWork.YaddaSaxlaAsync();

            // İlkin IsciTeyinat yarat
            var teyinat = new IsciTeyinat
            {
                IsciId = isci.Id,
                DepartamentId = dto.DepartamentId,
                VezifeId = dto.VezifeId,
                BaslamaTarixi = dto.IsheQebulTarixi,
                Esasdir = true,
                Aktivdir = true
            };
            await _unitOfWork.Repository<IsciTeyinat>().YaratAsync(teyinat);

            // İlkin IsciMaliye yarat
            var maliye = new IsciMaliye
            {
                IsciId = isci.Id,
                CariMaas = dto.BaslangicMaas ?? 0
            };
            await _unitOfWork.Repository<IsciMaliye>().YaratAsync(maliye);

            // 3 növ məzuniyyət balansı yarat: İllik, Xəstəlik, Ezamiyyət
            var illikBalans = new MezuniyyetBalans
            {
                IsciId = isci.Id,
                Il = DateTime.Now.Year,
                Nov = MezuniyyetNovu.Illik,
                ToplamGun = dto.BaslangicMezuniyyet ?? 21
            };
            var xestelikBalans = new MezuniyyetBalans
            {
                IsciId = isci.Id,
                Il = DateTime.Now.Year,
                Nov = MezuniyyetNovu.Xestelik,
                ToplamGun = 0 // limitsiz
            };
            var ezamiyyetBalans = new MezuniyyetBalans
            {
                IsciId = isci.Id,
                Il = DateTime.Now.Year,
                Nov = MezuniyyetNovu.Ezamiyyet,
                ToplamGun = 0 // limitsiz
            };

            await _unitOfWork.Repository<MezuniyyetBalans>().YaratAsync(illikBalans);
            await _unitOfWork.Repository<MezuniyyetBalans>().YaratAsync(xestelikBalans);
            await _unitOfWork.Repository<MezuniyyetBalans>().YaratAsync(ezamiyyetBalans);

            await _unitOfWork.YaddaSaxlaAsync();

            // Yaradılmış işçini əlaqələri ilə yüklə
            var createdIsci = await _unitOfWork.Repository<Isci>().GetirAsync(
                x => x.Id == isci.Id,
                include: q => q
                    .Include(x => x.IsciTeyinatlari).ThenInclude(t => t.Departament)
                    .Include(x => x.IsciTeyinatlari).ThenInclude(t => t.Vezife)
                    .Include(x => x.Maliye)
            );

            return Result<IsciListDto>.Ok(_mapper.Map<IsciListDto>(createdIsci ?? isci));
        }
        catch (Exception ex)
        {
            return Result<IsciListDto>.Fail($"İşçi yaradılarkən xəta baş verdi: {ex.Message}");
        }
    }

    public async Task<IsciDetailDto?> GetIsciDetailsAsync(int id)
    {
        var entity = await _unitOfWork.Repository<Isci>().GetirAsync(
            x => x.Id == id,
            include: q => q
                .Include(x => x.IsciTeyinatlari).ThenInclude(t => t.Departament)
                .Include(x => x.IsciTeyinatlari).ThenInclude(t => t.Vezife)
                .Include(x => x.Maliye)
        );
        return _mapper.Map<IsciDetailDto>(entity);
    }

    public async Task<IList<IsciListDto>> GetIscilerBySobeIdAsync(int sobeId)
    {
        var entities = await _unitOfWork.Repository<Isci>().HamisiniGetirAsync(
            x => x.IsciTeyinatlari.Any(t => t.Aktivdir && t.DepartamentId == sobeId),
            include: q => q
                .Include(x => x.IsciTeyinatlari).ThenInclude(t => t.Departament)
                .Include(x => x.IsciTeyinatlari).ThenInclude(t => t.Vezife)
                .Include(x => x.Maliye),
            izlemeden: true
        );
        return _mapper.Map<IList<IsciListDto>>(entities);
    }

    public async Task<bool> CheckFinExistsAsync(string fin) =>
        await _unitOfWork.Repository<Isci>().MovcuddurmuAsync(x => x.FIN == fin);

    public async Task<Result<List<IsciListDto>>> SearchIscilerByFinAsync(string fin)
    {
        var entities = await _unitOfWork.Repository<Isci>().HamisiniGetirAsync(
            x => x.FIN.Contains(fin),
            include: q => q
                .Include(x => x.IsciTeyinatlari).ThenInclude(t => t.Departament)
                .Include(x => x.Maliye),
            izlemeden: true
        );
        return Result<List<IsciListDto>>.Ok(_mapper.Map<List<IsciListDto>>(entities));
    }

    public async Task<Result> UpdateSalaryWithHistoryAsync(int isciId, decimal yeniMaas, string? emrNo)
    {
        try
        {
            var isci = await _unitOfWork.Repository<Isci>().IdIleGetirAsync(isciId);
            if (isci == null) return Result.Fail("İşçi tapılmadı.");

            // Əmr nömrəsini avtomatik yarat
            if (string.IsNullOrWhiteSpace(emrNo))
            {
                var il = DateTime.Now.Year;
                var umumiSayi = await _unitOfWork.Repository<IsciMaasTarixcesi>()
                    .SayAsync(x => x.DeyismeTarixi.Year == il);
                emrNo = $"EMR-{il}/{(umumiSayi + 1):D3}";
            }

            // Maliye-ni yüklə
            var maliye = await _unitOfWork.Repository<IsciMaliye>()
                .GetirAsync(x => x.IsciId == isciId);

            var kohneMaas = maliye?.CariMaas ?? 0;

            // Tarixçə yarat
            var tarixce = new IsciMaasTarixcesi
            {
                IsciId = isciId,
                KohneMaas = kohneMaas,
                YeniMaas = yeniMaas,
                DeyismeTarixi = DateTime.Now,
                EmrinNomresi = emrNo
            };
            await _unitOfWork.Repository<IsciMaasTarixcesi>().YaratAsync(tarixce);

            if (maliye != null)
            {
                maliye.CariMaas = yeniMaas;
                maliye.YenilenmeTarixi = DateTime.Now;
                await _unitOfWork.Repository<IsciMaliye>().YenileAsync(maliye);
            }
            else
            {
                var yeniMaliye = new IsciMaliye { IsciId = isciId, CariMaas = yeniMaas };
                await _unitOfWork.Repository<IsciMaliye>().YaratAsync(yeniMaliye);
            }

            await _unitOfWork.YaddaSaxlaAsync();
            return Result.Ok("Maaş uğurla yeniləndi.");
        }
        catch (Exception ex)
        {
            return Result.Fail($"Maaş yenilənmədi: {ex.Message}");
        }
    }

    public async Task<Result<IList<IsciMaasTarixcesiDto>>> GetMaasTarixcesiAsync(int isciId)
    {
        try
        {
            var list = await _unitOfWork.Repository<IsciMaasTarixcesi>()
                .HamisiniGetirAsync(x => x.IsciId == isciId, izlemeden: true);

            var dto = list.OrderByDescending(x => x.DeyismeTarixi)
                         .Select(x => new IsciMaasTarixcesiDto
                         {
                             Id = x.Id,
                             IsciId = x.IsciId,
                             KohneMaas = x.KohneMaas,
                             YeniMaas = x.YeniMaas,
                             DeyismeTarixi = x.DeyismeTarixi,
                             EmrinNomresi = x.EmrinNomresi
                         }).ToList();

            return Result<IList<IsciMaasTarixcesiDto>>.Ok(dto);
        }
        catch
        {
            return Result<IList<IsciMaasTarixcesiDto>>.Fail("Maaş tarixçəsi gətirilərkən xəta baş verdi.");
        }
    }

    public async Task<Result> TeyinatDeyisAsync(int isciId, int departamentId, int vezifeId, DateTime baslamaTarixi)
    {
        try
        {
            var kohne = await _unitOfWork.Repository<IsciTeyinat>()
                .GetirAsync(x => x.IsciId == isciId && x.Aktivdir);

            if (kohne != null)
            {
                kohne.Aktivdir = false;
                kohne.BitmeTarixi = baslamaTarixi;
                await _unitOfWork.Repository<IsciTeyinat>().YenileAsync(kohne);
            }

            var yeniTeyinat = new IsciTeyinat
            {
                IsciId = isciId,
                DepartamentId = departamentId,
                VezifeId = vezifeId,
                BaslamaTarixi = baslamaTarixi,
                Esasdir = true,
                Aktivdir = true
            };
            await _unitOfWork.Repository<IsciTeyinat>().YaratAsync(yeniTeyinat);

            return await _unitOfWork.YaddaSaxlaAsync() > 0 ? Result.Ok() : Result.Fail("Xəta baş verdi.");
        }
        catch (Exception ex)
        {
            return Result.Fail($"Təyinat dəyişikliyi zamanı xəta: {ex.Message}");
        }
    }

    public async Task<Result> TeyinatRedakteEtAsync(int isciId, int departamentId, int vezifeId)
    {
        try
        {
            // Aktiv təyinatı tap
            var repo = _unitOfWork.Repository<IsciTeyinat>();
            var aktiv = await repo.GetirAsync(x => x.IsciId == isciId && x.Aktivdir);

            if (aktiv == null)
            {
                // Aktiv təyinat yoxdur - yeni yaradaq
                var yeni = new IsciTeyinat
                {
                    IsciId = isciId,
                    DepartamentId = departamentId,
                    VezifeId = vezifeId,
                    BaslamaTarixi = DateTime.Today,
                    Esasdir = true,
                    Aktivdir = true
                };
                await repo.YaratAsync(yeni);
                await _unitOfWork.YaddaSaxlaAsync();
                return Result.Ok("Yeni təyinat yaradıldı.");
            }

            // Dəyişiklik varmı yoxla
            if (aktiv.DepartamentId == departamentId && aktiv.VezifeId == vezifeId)
                return Result.Ok("Dəyişiklik yoxdur.");

            // Entity-ni ID ilə yenidən yüklə və yenilə
            var entity = await repo.IdIleGetirAsync(aktiv.Id);
            if (entity == null)
                return Result.Fail("Təyinat tapılmadı.");

            entity.DepartamentId = departamentId;
            entity.VezifeId = vezifeId;
            entity.YenilenmeTarixi = DateTime.Now;

            var saved = await _unitOfWork.YaddaSaxlaAsync();
            return saved > 0
                ? Result.Ok("Təyinat uğurla redaktə edildi.")
                : Result.Fail($"Bazaya yazılmadı. (saved={saved})");
        }
        catch (Exception ex)
        {
            return Result.Fail($"Təyinat redaktəsi zamanı xəta: {ex.Message}");
        }
    }

    public async Task<Result<int?>> GetAktivDepartamentIdAsync(int isciId)
    {
        try
        {
            var isci = await _unitOfWork.Repository<Isci>()
                .GetirAsync(
                    predicate: x => x.Id == isciId,
                    include: q => q.Include(i => i.IsciTeyinatlari));

            if (isci == null)
                return Result<int?>.Fail("İşçi tapılmadı.");

            var departamentId = isci.IsciTeyinatlari
                .Where(t => t.Aktivdir)
                .Select(t => (int?)t.DepartamentId)
                .FirstOrDefault();

            return Result<int?>.Ok(departamentId);
        }
        catch
        {
            return Result<int?>.Fail("Xəta baş verdi.");
        }
    }

    public async Task<decimal> GetCariMaasAsync(int isciId)
    {
        var maliye = await _unitOfWork.Repository<IsciMaliye>()
            .GetirAsync(x => x.IsciId == isciId, izlemeden: true);
        return maliye?.CariMaas ?? 0;
    }
}
