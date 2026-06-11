using FinNex.Domain.Entities.HR;

namespace FinNex.Application.Services.HR
{
    // Mərkəzi əmr nömrələmə + reyestr. Bütün HR əmr növləri (məzuniyyət, maaş
    // dəyişikliyi, işə qəbul, xitam, ezamiyyət) buradan istifadə edir.
    public interface IEmrService
    {
        // (Nov, Il) sayğacını artırır və növbəti nömrəni qaytarır.
        Task<(int nomre, int il)> NovbetiNomreAsync(EmrNovu nov, DateTime? tarix = null);

        // Yeni əmr reyestr sətri yaradır (nömrə avtomatik sayğacdan) və qaytarır.
        Task<Emr> YeniEmrAsync(EmrNovu nov, int? elaqeliEntityId = null, DateTime? tarix = null,
                               string? metn = null, int? verenIsciId = null);

        // HR son nömrəni əl ilə təyin edir (köhnə kağız əmrlərin davamı üçün).
        Task SonNomreTeyinEtAsync(EmrNovu nov, int il, int sonNomre);

        // Sayğacların cari vəziyyəti (idarəetmə səhifəsi üçün).
        Task<IList<EmrSayghaci>> SayghaclarAsync();
    }
}
