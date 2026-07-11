using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Emeliyyat;

namespace FinNex.Application.Interfaces.Emeliyyat;

public interface ITelebeKocurmeService
{
    // Standart hesab nömrələri (form default-ları)
    (string h35025, string h45023, string h45011, string h67013) StandartHesablar();

    // Komissiya hesabla: Mebleg × Kurs × XH / 100, minimum 0.5
    decimal KomissiyaHesabla(decimal? mebleg, decimal? kurs, decimal? xh);

    Task<IList<TelebeKocurmeListDto>> HamisiniGetirAsync(int? il = null);
    Task<TelebeKocurmeDetalDto?> DetalAsync(int id);

    // Form dəyərlərindən canlı 3 debet/kredit sətri (yadda saxlanmadan)
    IList<MuhasibatSetirDto> SetirlerHesabla(TelebeKocurmeFormDto dto);

    Task<Result<int>> YaratAsync(TelebeKocurmeCreateDto dto, int yaradanUserId);

    // Mövcud qeydi təkrar üçün: məlumat dolu, yeni № (əl ilə) + bugünkü tarix
    Task<TelebeKocurmeCreateDto?> TekrarMelumatiAsync(int id);

    Task<TelebeKocurmeEditDto?> RedakteMelumatiAsync(int id);
    Task<Result> YenileAsync(TelebeKocurmeEditDto dto, int userId, bool isAdmin);
    Task<Result> SilAsync(int id, int userId, bool isAdmin);
}
