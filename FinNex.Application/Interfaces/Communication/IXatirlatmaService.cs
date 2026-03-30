// Application/Interfaces/Communication/IXatirlatmaService.cs
using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Communication;

namespace FinNex.Application.Interfaces.Communication
{
    public interface IXatirlatmaService
    {
        // İşçinin bütün xatırlatmaları (Index səhifəsi)
        Task<Result<IList<XatirlatmaListDto>>> GetIsciXatirlatmalariAsync(int isciId);

        // Dashboard badge sayı — vaxtı gəlmiş, oxunmamış xatırlatmalar
        Task<Result<int>> OxunmamisSayiAsync(int isciId);

        // İşçi özü şəxsi xatırlatma yaradır
        Task<Result> YaratAsync(XatirlatmaCreateDto dto);

        // BackgroundService sistem xatırlatması yaradır
        Task<Result> SistemXatirlatmasiYaratAsync(XatirlatmaSistemCreateDto dto);

        // Tək xatırlatmanı oxundu işarələ (AJAX)
        Task<Result> OxunduIsareEtAsync(int id, int isciId);

        // Hamısını oxundu işarələ (AJAX)
        Task<Result> HamisiniOxunduIsareEtAsync(int isciId);

        // Şəxsi xatırlatmanı sil (sistem xatırlatması silinmir)
        Task<Result> SilAsync(int id, int isciId);

        // BackgroundService — vaxtı gəlmiş, hələ göndərilməmiş xatırlatmalar
        Task<Result<IList<XatirlatmaListDto>>> GetGonderilemeyenlerAsync();

        // BackgroundService — göndərildikdən sonra işarələ
        Task<Result> GonderildiIsareEtAsync(int id);
    }
}