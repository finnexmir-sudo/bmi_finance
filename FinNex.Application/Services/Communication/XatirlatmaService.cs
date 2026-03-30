// Application/Services/Communication/XatirlatmaService.cs
using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Communication;
using FinNex.Application.Interfaces.Communication;
using FinNex.Domain.Entities.Communication;
using FinNex.Domain.Interfaces;

namespace FinNex.Application.Services.Communication
{
    public class XatirlatmaService : IXatirlatmaService
    {
        private readonly IUnitOfWork _unitOfWork;

        public XatirlatmaService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<IList<XatirlatmaListDto>>> GetIsciXatirlatmalariAsync(int isciId)
        {
            try
            {
                var list = await _unitOfWork.Repository<Xatirlatma>()
                .HamisiniGetirAsync(
                    x => x.IsciId == isciId,
                    izlemeden: true
                );

                var siralanmis = list
                    .OrderByDescending(x => x.XatirlatmaTarixi)
                    .ToList();

                var dtos = siralanmis.Select(MapToDto).ToList();
                return Result<IList<XatirlatmaListDto>>.Ok(dtos);
            }
            catch (Exception ex)
            {
                return Result<IList<XatirlatmaListDto>>.Fail("Xatırlatmalar yüklənmədi: " + ex.Message);
            }
        }

        public async Task<Result<int>> OxunmamisSayiAsync(int isciId)
        {
            try
            {
                var say = await _unitOfWork.Repository<Xatirlatma>()
                    .SayAsync(x => x.IsciId == isciId
                                && !x.OxunubMu
                                && x.XatirlatmaTarixi <= DateTime.Now);
                return Result<int>.Ok(say);
            }
            catch (Exception ex)
            {
                return Result<int>.Fail(ex.Message);
            }
        }

        public async Task<Result> YaratAsync(XatirlatmaCreateDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Bashliq))
                    return Result.Fail("Başlıq boş ola bilməz.");

                if (dto.XatirlatmaTarixi <= DateTime.Now)
                    return Result.Fail("Xatırlatma tarixi gələcəkdə olmalıdır.");

                var entity = new Xatirlatma
                {
                    IsciId = dto.IsciId,
                    Bashliq = dto.Bashliq,
                    Qeyd = dto.Qeyd,
                    XatirlatmaTarixi = dto.XatirlatmaTarixi,
                    Nov = XatirlatmaNov.Fərdi
                };

                await _unitOfWork.Repository<Xatirlatma>().YaratAsync(entity);
                await _unitOfWork.YaddaSaxlaAsync();

                return Result.Ok("Xatırlatma yaradıldı.");
            }
            catch (Exception ex)
            {
                return Result.Fail("Xəta: " + ex.Message);
            }
        }

        public async Task<Result> SistemXatirlatmasiYaratAsync(XatirlatmaSistemCreateDto dto)
        {
            try
            {
                // Eyni entity üçün artıq sistem xatırlatması varsa, yenidən yaratma
                var movcud = await _unitOfWork.Repository<Xatirlatma>()
                    .GetirAsync(x => x.IsciId == dto.IsciId
                                  && x.EntityTipi == dto.EntityTipi
                                  && x.EntityId == dto.EntityId
                                  && x.Nov == XatirlatmaNov.Sistem,
                                izlemeden: true);

                if (movcud != null) return Result.Ok("Artıq mövcuddur.");

                var entity = new Xatirlatma
                {
                    IsciId = dto.IsciId,
                    Bashliq = dto.Bashliq,
                    Qeyd = dto.Qeyd,
                    XatirlatmaTarixi = dto.XatirlatmaTarixi,
                    Nov = XatirlatmaNov.Sistem,
                    EntityTipi = dto.EntityTipi,
                    EntityId = dto.EntityId
                };

                await _unitOfWork.Repository<Xatirlatma>().YaratAsync(entity);
                await _unitOfWork.YaddaSaxlaAsync();

                return Result.Ok("Sistem xatırlatması yaradıldı.");
            }
            catch (Exception ex)
            {
                return Result.Fail("Xəta: " + ex.Message);
            }
        }

        public async Task<Result> OxunduIsareEtAsync(int id, int isciId)
        {
            try
            {
                var entity = await _unitOfWork.Repository<Xatirlatma>()
                    .GetirAsync(x => x.Id == id && x.IsciId == isciId);

                if (entity == null) return Result.Fail("Tapılmadı.");

                entity.OxunubMu = true;
                _unitOfWork.Repository<Xatirlatma>().YenileAsync(entity);
                await _unitOfWork.YaddaSaxlaAsync();

                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Fail(ex.Message);
            }
        }

        public async Task<Result> HamisiniOxunduIsareEtAsync(int isciId)
        {
            try
            {
                var list = await _unitOfWork.Repository<Xatirlatma>()
                    .HamisiniGetirAsync(x => x.IsciId == isciId && !x.OxunubMu);

                foreach (var x in list)
                {
                    x.OxunubMu = true;
                    _unitOfWork.Repository<Xatirlatma>().YenileAsync(x);
                }

                await _unitOfWork.YaddaSaxlaAsync();
                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Fail(ex.Message);
            }
        }

        public async Task<Result> SilAsync(int id, int isciId)
        {
            try
            {
                var entity = await _unitOfWork.Repository<Xatirlatma>()
                    .GetirAsync(x => x.Id == id && x.IsciId == isciId);

                if (entity == null) return Result.Fail("Tapılmadı.");

                if (entity.Nov == XatirlatmaNov.Sistem)
                    return Result.Fail("Sistem xatırlatmaları silinə bilməz.");

                _unitOfWork.Repository<Xatirlatma>().DeleteAsync(entity.Id);
                await _unitOfWork.YaddaSaxlaAsync();

                return Result.Ok("Silindi.");
            }
            catch (Exception ex)
            {
                return Result.Fail(ex.Message);
            }
        }

        public async Task<Result<IList<XatirlatmaListDto>>> GetGonderilemeyenlerAsync()
        {
            try
            {
                var list = await _unitOfWork.Repository<Xatirlatma>()
                    .HamisiniGetirAsync(
                        x => !x.GonderiilibMi && x.XatirlatmaTarixi <= DateTime.Now,
                        izlemeden: true);

                return Result<IList<XatirlatmaListDto>>.Ok(list.Select(MapToDto).ToList());
            }
            catch (Exception ex)
            {
                return Result<IList<XatirlatmaListDto>>.Fail(ex.Message);
            }
        }

        public async Task<Result> GonderildiIsareEtAsync(int id)
        {
            try
            {
                var entity = await _unitOfWork.Repository<Xatirlatma>()
                    .GetirAsync(x => x.Id == id);

                if (entity == null) return Result.Fail("Tapılmadı.");

                entity.GonderiilibMi = true;
                _unitOfWork.Repository<Xatirlatma>().YenileAsync(entity);
                await _unitOfWork.YaddaSaxlaAsync();

                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Fail(ex.Message);
            }
        }

        private static XatirlatmaListDto MapToDto(Xatirlatma x) => new()
        {
            Id = x.Id,
            Bashliq = x.Bashliq,
            Qeyd = x.Qeyd,
            XatirlatmaTarixi = x.XatirlatmaTarixi,
            OxunubMu = x.OxunubMu,
            Nov = (int)x.Nov,
            EntityTipi = x.EntityTipi.HasValue ? (int)x.EntityTipi.Value : null,
            EntityId = x.EntityId
        };
    }
}