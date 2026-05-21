using FinNex.Domain.Entities.Pid;

namespace FinNex.Application.Interfaces.Pid;

public interface IPidSmsSablonService
{
    Task<IList<PidSmsSablon>> HamisiniGetirAsync(bool yalnizAktiv = false);
    Task<PidSmsSablon?> IdIleGetirAsync(int id);
    Task<PidSmsSablon> YaratAsync(string ad, string metn, string? aciqlama, int yaradanIsciId);
    Task<bool> YenileAsync(int id, string ad, string metn, string? aciqlama, bool aktiv, int yenileyenIsciId);
    Task<bool> SilAsync(int id, int silenIsciId);
}
