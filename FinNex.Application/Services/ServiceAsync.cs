using AutoMapper;
using FinNex.Application.Interfaces;
using FinNex.Domain.Interfaces;

namespace FinNex.Application.Services
{
    public class ServiceAsync<TEntity, TDto, TCreateDto, TUpdateDto> : IServiceAsync<TEntity, TDto, TCreateDto, TUpdateDto>
    where TEntity : BaseEntity
    where TDto : class
    {
        protected readonly IUnitOfWork _unitOfWork;
        protected readonly IMapper _mapper;

        public ServiceAsync(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IList<TDto>> HamisiniGetirAsync()
        {
            var entities = await _unitOfWork.Repository<TEntity>().HamisiniGetirAsync(izlemeden: true);
            return _mapper.Map<IList<TDto>>(entities);
        }

        public async Task<TDto?> IdIleGetirAsync(int id)
        {
            var entity = await _unitOfWork.Repository<TEntity>().IdIleGetirAsync(id);
            return _mapper.Map<TDto>(entity);
        }

        public async Task<TDto> YaratAsync(TCreateDto dto)
        {
            var entity = _mapper.Map<TEntity>(dto);
            await _unitOfWork.Repository<TEntity>().YaratAsync(entity);
            await _unitOfWork.YaddaSaxlaAsync();
            return _mapper.Map<TDto>(entity);
        }

        public async Task YenileAsync(TUpdateDto dto)
        {
            var entity = _mapper.Map<TEntity>(dto);
            await _unitOfWork.Repository<TEntity>().YenileAsync(entity);
            await _unitOfWork.YaddaSaxlaAsync();
        }

        public async Task SilAsync(int id)
        {
            await _unitOfWork.Repository<TEntity>().YumshakSilAsync(id);
            await _unitOfWork.YaddaSaxlaAsync();
        }
    }
}
