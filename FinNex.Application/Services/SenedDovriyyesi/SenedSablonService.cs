using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.SenedDovriyyesi.SenedSablon;
using FinNex.Application.Interfaces.SenedDovriyyesi;
using FinNex.DataAccess.UnitOfWorks;
using FinNex.Domain.Entities.SenedDovriyyesi;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.SenedDovriyyesi
{
    public class SenedSablonService : ISenedSablonService
    {
        private readonly IUnitOfWork _uow;
        private readonly IFileStorageService _storage;

        public SenedSablonService(IUnitOfWork uow, IFileStorageService storage)
        {
            _uow = uow;
            _storage = storage;
        }

        // ================================
        // Şablonları siyahıla (istəyə görə sənəd növünə görə filtr)
        // ================================
        public async Task<Result<List<SenedSablonListDto>>> GetListAsync(int? senedNovuId = null)
        {
            var query = _uow.Repository<SenedSablon>()
                .Query()
                .Include(x => x.SenedNovu)
                .Where(x => !x.Silinib);

            if (senedNovuId.HasValue)
                query = query.Where(x => x.SenedNovuId == senedNovuId.Value);

            var entities = await query
                .OrderByDescending(x => x.YaradilmaTarixi)
                .ToListAsync();

            var dtoList = entities.Select(x => new SenedSablonListDto
            {
                Id = x.Id,
                Ad = x.Ad,
                Tesvir = x.Tesvir,
                SenedNovuId = x.SenedNovuId,
                SenedNovuAd = x.SenedNovu.Ad,
                FaylAdi = x.FaylAdi,
                Aktiv = x.Aktiv,
                YaradilmaTarixi = x.YaradilmaTarixi
            }).ToList();

            return Result<List<SenedSablonListDto>>.Ok(dtoList);
        }

        // ================================
        // Şablon yarat (fayl yüklə)
        // ================================
        public async Task<Result<int>> CreateAsync(
            SenedSablonCreateDto dto,
            Stream faylStream,
            string faylAdi,
            string contentType,
            int userId)
        {
            if (string.IsNullOrWhiteSpace(dto.Ad))
                return Result<int>.Fail("Şablon adı boş ola bilməz.");

            var nov = await _uow.Repository<SenedNovu>()
                .GetirAsync(x => x.Id == dto.SenedNovuId && !x.Silinib);

            if (nov is null)
                return Result<int>.Fail("Sənəd növü tapılmadı.");

            // Faylı storage-ə yaz
            var (storedName, path, _) = await _storage.SaveAsync(faylStream, faylAdi, contentType);

            var entity = new SenedSablon
            {
                Ad = dto.Ad.Trim(),
                Tesvir = dto.Tesvir?.Trim(),
                SenedNovuId = dto.SenedNovuId,
                FaylYolu = path,
                FaylAdi = faylAdi,
                Aktiv = true,
                YaradanIcraciId = userId
            };

            await _uow.Repository<SenedSablon>().YaratAsync(entity);
            await _uow.YaddaSaxlaAsync();

            return Result<int>.Ok(entity.Id, "Şablon yaradıldı.");
        }

        // ================================
        // Soft Delete
        // ================================
        public async Task<Result> DeleteAsync(int id, int userId)
        {
            var entity = await _uow.Repository<SenedSablon>()
                .IdIleGetirAsync(id);

            if (entity == null || entity.Silinib)
                return Result.Fail("Şablon tapılmadı.");

            entity.Silinib = true;
            entity.SilinmeTarixi = DateTime.Now;
            entity.SilenIcraciId = userId;

            await _uow.YaddaSaxlaAsync();

            return Result.Ok("Şablon silindi.");
        }

        // ================================
        // Fayl yolunu qaytarır (endirmə / kopyalama üçün)
        // ================================
        public async Task<Result<(string faylYolu, string faylAdi)>> GetFaylAsync(int id)
        {
            var entity = await _uow.Repository<SenedSablon>()
                .GetirAsync(x => x.Id == id && !x.Silinib);

            if (entity is null)
                return Result<(string faylYolu, string faylAdi)>.Fail("Şablon tapılmadı.");

            return Result<(string faylYolu, string faylAdi)>.Ok((entity.FaylYolu, entity.FaylAdi));
        }
    }
}
