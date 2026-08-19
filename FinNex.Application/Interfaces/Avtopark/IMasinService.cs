using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Avtopark;

namespace FinNex.Application.Interfaces.Avtopark
{
    /// <summary>Maşın kartı (CRUD) — Admin / Təsərrüfat.</summary>
    public interface IMasinService
    {
        /// <param name="yalnizAktiv">
        /// true — yalnız `MasinStatus.Aktiv` maşınlar (müraciət formasının siyahısı).
        /// Təmirdə/istifadədən çıxmış maşına müraciət yazıla bilməz.
        /// </param>
        Task<IList<MasinDto>> HamisiniGetirAsync(bool yalnizAktiv = false);

        Task<MasinDto?> GetirAsync(int id);

        Task<Result<int>> YaratAsync(MasinCreateDto dto, int userId);
        Task<Result> YenileAsync(MasinCreateDto dto, int userId);
        Task<Result> SilAsync(int id, int userId);
    }
}
