// GorushService.cs
using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Communication;
using FinNex.Application.Interfaces.Communication;
using FinNex.Domain.Entities.Communication;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.Communication
{
    public class GorushService : IGorushService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IBildirisService _bildirisService;

        public GorushService(IUnitOfWork unitOfWork, IBildirisService bildirisService)
        {
            _unitOfWork = unitOfWork;
            _bildirisService = bildirisService;
        }

        public async Task<Result<IList<GorushListDto>>> GetMenimGorushlerimAsync(int isciId)
        {
            try
            {
                var list = await _unitOfWork.Repository<GorushIshtirakci>()
                    .HamisiniGetirAsync(
                        predicate: x => x.IsciId == isciId && !x.Silinib,
                        include: q => q
                            .Include(gi => gi.Gorush)
                                .ThenInclude(g => g.TeshkilatciIsci)
                            .Include(gi => gi.Gorush)
                                .ThenInclude(g => g.Ishtirakcılar),
                        izlemeden: true);

                var dtos = list
                    .Where(gi => !gi.Gorush.Silinib)
                    .OrderBy(gi => gi.Gorush.Tarix)
                    .ThenBy(gi => gi.Gorush.BaslamaSaati)
                    .Select(gi => ToListDto(gi.Gorush, isciId, gi.Status))
                    .ToList();

                return Result<IList<GorushListDto>>.Ok(dtos);
            }
            catch (Exception ex)
            {
                return Result<IList<GorushListDto>>.Fail($"Xəta: {ex.Message}");
            }
        }

        public async Task<Result<IList<GorushListDto>>> GetTeshkilEtdiklerimAsync(int isciId)
        {
            try
            {
                var list = await _unitOfWork.Repository<Gorush>()
                    .HamisiniGetirAsync(
                        predicate: x => x.TeshkilatciIsciId == isciId && !x.Silinib,
                        include: q => q
                            .Include(g => g.TeshkilatciIsci)
                            .Include(g => g.Ishtirakcılar),
                        izlemeden: true);

                return Result<IList<GorushListDto>>.Ok(
                    list.OrderBy(x => x.Tarix)
                        .ThenBy(x => x.BaslamaSaati)
                        .Select(g => ToListDto(g, isciId, null)).ToList());
            }
            catch (Exception ex)
            {
                return Result<IList<GorushListDto>>.Fail($"Xəta: {ex.Message}");
            }
        }

        public async Task<Result<GorushDetailDto>> GetDetayAsync(int id, int isciId)
        {
            try
            {
                var g = await _unitOfWork.Repository<Gorush>()
                    .GetirAsync(
                        predicate: x => x.Id == id && !x.Silinib,
                        include: q => q
                            .Include(g => g.TeshkilatciIsci)
                            .Include(g => g.Ishtirakcılar)
                                .ThenInclude(gi => gi.Isci)
                                    .ThenInclude(i => i.IsciTeyinatlari)
                                        .ThenInclude(t => t.Vezife));

                if (g == null) return Result<GorushDetailDto>.Fail("Tapılmadı.");

                var menimStatusum = g.Ishtirakcılar
                    .FirstOrDefault(gi => gi.IsciId == isciId)?.Status;

                var dto = new GorushDetailDto
                {
                    Id = g.Id,
                    Bashliq = g.Bashliq,
                    Agenda = g.Agenda,
                    TeshkilatciAd = g.TeshkilatciIsci.TamAd,
                    TeshkilatciIsciId = g.TeshkilatciIsciId,
                    Tarix = g.Tarix,
                    BaslamaSaati = g.BaslamaSaati,
                    BitisSaati = g.BitisSaati,
                    Yer = g.Yer,
                    OnlineLink = g.OnlineLink,
                    Nov = g.Nov,
                    Status = g.Status,
                    Qeydler = g.Qeydler,
                    IshtirakciSayi = g.Ishtirakcılar.Count(gi => !gi.Silinib),
                    MenimIshtirakimVar = g.TeshkilatciIsciId == isciId ||
                                        g.Ishtirakcılar.Any(gi => gi.IsciId == isciId),
                    MenimStatusum = menimStatusum,
                    Ishtirakcılar = g.Ishtirakcılar
                        .Where(gi => !gi.Silinib)
                        .Select(gi => new GorushIshtirakciDto
                        {
                            Id = gi.Id,
                            IsciId = gi.IsciId,
                            IsciAd = gi.Isci.TamAd,
                            IsciVezife = gi.Isci.IsciTeyinatlari
                                .Where(t => t.Aktivdir)
                                .Select(t => t.Vezife.Ad)
                                .FirstOrDefault() ?? "-",
                            Status = gi.Status,
                            Qeyd = gi.Qeyd
                        }).ToList()
                };

                return Result<GorushDetailDto>.Ok(dto);
            }
            catch (Exception ex)
            {
                return Result<GorushDetailDto>.Fail($"Xəta: {ex.Message}");
            }
        }

        public async Task<Result> YaratAsync(GorushCreateDto dto)
        {
            try
            {
                var entity = new Gorush
                {
                    Bashliq = dto.Bashliq,
                    Agenda = dto.Agenda,
                    TeshkilatciIsciId = dto.TeshkilatciIsciId,
                    Tarix = dto.Tarix,
                    BaslamaSaati = dto.BaslamaSaati,
                    BitisSaati = dto.BitisSaati,
                    Yer = dto.Yer,
                    OnlineLink = dto.OnlineLink,
                    Nov = dto.Nov,
                    Status = GorushStatus.Planlanib
                };

                await _unitOfWork.Repository<Gorush>().YaratAsync(entity);
                await _unitOfWork.YaddaSaxlaAsync();

                // İştirakçıları əlavə et
                foreach (var isciId in dto.IshtirakciIsciIdler)
                {
                    var ishtirakci = new GorushIshtirakci
                    {
                        GorushId = entity.Id,
                        IsciId = isciId,
                        Status = IshtirakciStatus.Gozleyir
                    };
                    await _unitOfWork.Repository<GorushIshtirakci>().YaratAsync(ishtirakci);

                    // Hər iştirakçıya bildiriş
                    var teshkilatci = await _unitOfWork.Repository<Isci>()
                        .IdIleGetirAsync(dto.TeshkilatciIsciId);

                    await _bildirisService.YaratAsync(
                        isciId: isciId,
                        nov: BildirisNovu.YeniGorush,
                        bashliq: "Yeni görüşə dəvət",
                        metn: $"{teshkilatci?.TamAd} sizi '{dto.Bashliq}' görüşünə dəvət etdi. " +
                              $"{dto.Tarix:dd.MM.yyyy} {dto.BaslamaSaati:hh\\:mm}",
                        redirectUrl: $"/User/Gorush/Detay/{entity.Id}");
                }

                await _unitOfWork.YaddaSaxlaAsync();
                return Result.Ok("Görüş yaradıldı.");
            }
            catch (Exception ex)
            {
                return Result.Fail($"Xəta: {ex.Message}");
            }
        }

        public async Task<Result> StatusYenileAsync(int id, GorushStatus status,
            string? qeydler, int isciId)
        {
            var g = await _unitOfWork.Repository<Gorush>()
                .GetirAsync(x => x.Id == id);

            if (g == null) return Result.Fail("Tapılmadı.");
            if (g.TeshkilatciIsciId != isciId) return Result.Fail("Yalnız təşkilatçı dəyişə bilər.");

            g.Status = status;
            if (qeydler != null) g.Qeydler = qeydler;

            await _unitOfWork.Repository<Gorush>().YenileAsync(g);
            await _unitOfWork.YaddaSaxlaAsync();
            return Result.Ok("Yeniləndi.");
        }

        public async Task<Result> IshtirakciCavabAsync(int gorushId, int isciId,
            IshtirakciStatus status, string? qeyd)
        {
            var gi = await _unitOfWork.Repository<GorushIshtirakci>()
                .GetirAsync(x => x.GorushId == gorushId && x.IsciId == isciId);

            if (gi == null) return Result.Fail("Tapılmadı.");

            gi.Status = status;
            gi.Qeyd = qeyd;

            await _unitOfWork.Repository<GorushIshtirakci>().YenileAsync(gi);
            await _unitOfWork.YaddaSaxlaAsync();
            return Result.Ok("Cavab qeydə alındı.");
        }

        public async Task<Result> QeydEleveEtAsync(int gorushId, int isciId, string qeydler)
        {
            var g = await _unitOfWork.Repository<Gorush>()
                .GetirAsync(x => x.Id == gorushId);

            if (g == null) return Result.Fail("Tapılmadı.");
            if (g.TeshkilatciIsciId != isciId) return Result.Fail("Yalnız təşkilatçı qeyd əlavə edə bilər.");

            g.Qeydler = qeydler;
            await _unitOfWork.Repository<Gorush>().YenileAsync(g);
            await _unitOfWork.YaddaSaxlaAsync();
            return Result.Ok("Qeydlər saxlandı.");
        }

        public async Task<Result> SilAsync(int id, int isciId)
        {
            var g = await _unitOfWork.Repository<Gorush>()
                .GetirAsync(x => x.Id == id);

            if (g == null) return Result.Fail("Tapılmadı.");
            if (g.TeshkilatciIsciId != isciId) return Result.Fail("Yalnız təşkilatçı silə bilər.");

            await _unitOfWork.Repository<Gorush>().YumshakSilAsync(id);
            await _unitOfWork.YaddaSaxlaAsync();
            return Result.Ok("Silindi.");
        }

        private static GorushListDto ToListDto(Gorush g, int isciId,
            IshtirakciStatus? menimStatusum) => new()
            {
                Id = g.Id,
                Bashliq = g.Bashliq,
                TeshkilatciAd = g.TeshkilatciIsci?.TamAd ?? "",
                TeshkilatciIsciId = g.TeshkilatciIsciId,
                Tarix = g.Tarix,
                BaslamaSaati = g.BaslamaSaati,
                BitisSaati = g.BitisSaati,
                Yer = g.Yer,
                OnlineLink = g.OnlineLink,
                Nov = g.Nov,
                Status = g.Status,
                IshtirakciSayi = g.Ishtirakcılar?.Count(gi => !gi.Silinib) ?? 0,
                MenimIshtirakimVar = g.TeshkilatciIsciId == isciId ||
                                  g.Ishtirakcılar.Any(gi => gi.IsciId == isciId),
                MenimStatusum = menimStatusum
            };
        public async Task<Result> EditAsync(GorushEditDto dto)
        {
            try
            {
                var g = await _unitOfWork.Repository<Gorush>()
                    .GetirAsync(
                        predicate: x => x.Id == dto.Id,
                        include: q => q.Include(x => x.Ishtirakcılar));

                if (g == null) return Result.Fail("Tapılmadı.");
                if (g.TeshkilatciIsciId != dto.TeshkilatciIsciId)
                    return Result.Fail("Yalnız təşkilatçı redaktə edə bilər.");

                g.Bashliq = dto.Bashliq;
                g.Agenda = dto.Agenda;
                g.Tarix = dto.Tarix;
                g.BaslamaSaati = dto.BaslamaSaati;
                g.BitisSaati = dto.BitisSaati;
                g.Yer = dto.Yer;
                g.OnlineLink = dto.OnlineLink;
                g.Nov = dto.Nov;

                await _unitOfWork.Repository<Gorush>().YenileAsync(g);

                // İştirakçıları yenilə
                var kohneIds = g.Ishtirakcılar
                    .Where(x => !x.Silinib)
                    .Select(x => x.IsciId).ToList();

                // Yenilər — əlavə et
                var yeniIshtirakcilar = dto.IshtirakciIsciIdler
                    .Except(kohneIds).ToList();

                foreach (var isciId in yeniIshtirakcilar)
                {
                    await _unitOfWork.Repository<GorushIshtirakci>()
                        .YaratAsync(new GorushIshtirakci
                        {
                            GorushId = g.Id,
                            IsciId = isciId,
                            Status = IshtirakciStatus.Gozleyir
                        });

                    // Yeni iştirakçıya bildiriş
                    var teshkilatci = await _unitOfWork.Repository<Isci>()
                        .IdIleGetirAsync(dto.TeshkilatciIsciId);

                    await _bildirisService.YaratAsync(
                        isciId: isciId,
                        nov: BildirisNovu.YeniGorush,
                        bashliq: "Görüşə dəvət",
                        metn: $"{teshkilatci?.TamAd} sizi '{g.Bashliq}' görüşünə əlavə etdi.",
                        redirectUrl: $"/User/Gorush/Detay/{g.Id}");
                }

                // Çıxarılanlar — soft delete
                var cixarilanlar = kohneIds
                    .Except(dto.IshtirakciIsciIdler).ToList();

                foreach (var isciId in cixarilanlar)
                {
                    var ishtirakci = g.Ishtirakcılar
                        .FirstOrDefault(x => x.IsciId == isciId && !x.Silinib);
                    if (ishtirakci != null)
                        await _unitOfWork.Repository<GorushIshtirakci>()
                            .YumshakSilAsync(ishtirakci.Id);
                }

                await _unitOfWork.YaddaSaxlaAsync();

                // Mövcud iştirakçılara yeniləmə bildirişi
                foreach (var isciId in kohneIds
                    .Intersect(dto.IshtirakciIsciIdler).ToList())
                {
                    await _bildirisService.YaratAsync(
                        isciId: isciId,
                        nov: BildirisNovu.YeniGorush,
                        bashliq: "Görüş yeniləndi",
                        metn: $"'{g.Bashliq}' görüşünün məlumatları yeniləndi.",
                        redirectUrl: $"/User/Gorush/Detay/{g.Id}");
                }

                return Result.Ok("Görüş yeniləndi.");
            }
            catch (Exception ex)
            {
                return Result.Fail($"Xəta: {ex.Message}");
            }
        }
        public async Task<Result<IList<GorushDetailDto>>> GetYaxinlasanlarAsync(DateTime tarix)
        {
            try
            {
                var start = tarix.Date;
                var end = start.AddDays(1);

                var data = await _unitOfWork.Repository<Gorush>()
                    .HamisiniGetirAsync(
                        x => x.Tarix >= start && x.Tarix < end,
                        include: q => q.Include(g => g.Ishtirakcılar),
                        izlemeden: true
                    );

                var dto = data.Select(x => new GorushDetailDto
                {
                    Id = x.Id,
                    Bashliq = x.Bashliq,
                    Tarix = x.Tarix,
                    BaslamaSaati = x.BaslamaSaati,
                    Ishtirakcılar = x.Ishtirakcılar.Select(i => new GorushIshtirakciDto
                    {
                        IsciId = i.IsciId,
                        Status = i.Status
                    }).ToList()
                }).ToList();

                return Result<IList<GorushDetailDto>>.Ok(dto);
            }
            catch (Exception ex)
            {
                return Result<IList<GorushDetailDto>>.Fail("Görüşlər gətirilmədi: " + ex.Message);
            }
        }
    }
}