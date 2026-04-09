using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Communication;
using FinNex.Application.Interfaces.Communication;
using FinNex.Domain.Entities.Communication;
using FinNex.Domain.Interfaces;

namespace FinNex.Application.Services.Communication
{
    public class BildirisService : IBildirisService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebPushService _webPushService;

        public BildirisService(IUnitOfWork unitOfWork, IWebPushService webPushService)
        {
            _unitOfWork = unitOfWork;
            _webPushService = webPushService;
        }

        public async Task<Result<IList<BildirisDto>>> GetIscibildirisleriAsync(int isciId)
        {
            try
            {
                var list = await _unitOfWork.Repository<Bildiris>()
                    .HamisiniGetirAsync(
                        predicate: x => x.IsciId == isciId && !x.Silinib,
                        izlemeden: true);

                var dtos = list
                    .OrderByDescending(x => x.YaradilmaTarixi)
                    .Select(b => new BildirisDto
                    {
                        Id = b.Id,
                        Nov = b.Nov,
                        Bashliq = b.Bashliq,
                        Metn = b.Metn,
                        Oxunub = b.Oxunub,
                        OxunmaTarixi = b.OxunmaTarixi,
                        YaradilmaTarixi = b.YaradilmaTarixi,
                        RedirectUrl = b.RedirectUrl,
                        MezuniyyetId = b.MezuniyyetId,
                        IcazeId = b.IcazeId,
                        MesajId = b.MesajId
                    }).ToList();

                return Result<IList<BildirisDto>>.Ok(dtos);
            }
            catch (Exception ex)
            {
                return Result<IList<BildirisDto>>.Fail($"Xəta: {ex.Message}");
            }
        }

        public async Task<Result> OxunduIsareEtAsync(int bildirisId, int isciId)
        {
            var b = await _unitOfWork.Repository<Bildiris>()
                .GetirAsync(x => x.Id == bildirisId && x.IsciId == isciId);

            if (b == null) return Result.Fail("Tapılmadı.");

            b.Oxunub = true;
            b.OxunmaTarixi = DateTime.Now;

            await _unitOfWork.Repository<Bildiris>().YenileAsync(b);
            await _unitOfWork.YaddaSaxlaAsync();
            return Result.Ok();
        }

        public async Task<Result> HamisiniOxunduIsareEtAsync(int isciId)
        {
            var list = await _unitOfWork.Repository<Bildiris>()
                .HamisiniGetirAsync(x => x.IsciId == isciId && !x.Oxunub && !x.Silinib);

            foreach (var b in list)
            {
                b.Oxunub = true;
                b.OxunmaTarixi = DateTime.Now;
                await _unitOfWork.Repository<Bildiris>().YenileAsync(b);
            }

            await _unitOfWork.YaddaSaxlaAsync();
            return Result.Ok();
        }

        public async Task<Result<int>> OxunmamisSayiAsync(int isciId)
        {
            var sayi = (await _unitOfWork.Repository<Bildiris>()
                .HamisiniGetirAsync(x => x.IsciId == isciId && !x.Oxunub && !x.Silinib))
                .Count;

            return Result<int>.Ok(sayi);
        }

        public async Task<Result> YaratAsync(int isciId, BildirisNovu nov,
            string bashliq, string metn, string? redirectUrl = null,
            int? mezuniyyetId = null, int? icazeId = null, int? mesajId = null)
        {
            try
            {
                var entity = new Bildiris
                {
                    IsciId = isciId,
                    Nov = nov,
                    Bashliq = bashliq,
                    Metn = metn,
                    RedirectUrl = redirectUrl,
                    MezuniyyetId = mezuniyyetId,
                    IcazeId = icazeId,
                    MesajId = mesajId
                };

                await _unitOfWork.Repository<Bildiris>().YaratAsync(entity);
                await _unitOfWork.YaddaSaxlaAsync();

                // Web Push bildirişi göndər
                try
                {
                    await _webPushService.BildirisSonderAsync(isciId, bashliq, metn, redirectUrl);
                }
                catch { /* Push xətası əsas axını dayandırmamalıdır */ }

                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Fail($"Xəta: {ex.Message}");
            }
        }
    }
}
