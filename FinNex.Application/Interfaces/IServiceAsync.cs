
namespace FinNex.Application.Interfaces
{
    public interface IServiceAsync<TEntity, TDto, TCreateDto, TUpdateDto>
    where TEntity : class
    where TDto : class
    {
        Task<IList<TDto>> HamisiniGetirAsync();
        Task<TDto?> IdIleGetirAsync(int id);
        Task<TDto> YaratAsync(TCreateDto dto);
        Task YenileAsync(TUpdateDto dto);
        Task SilAsync(int id);
    }
}
