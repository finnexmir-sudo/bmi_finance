using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.Mezuniyyet;
using FinNex.Domain.Entities.HR;

namespace FinNex.Application.Interfaces
{
    public interface IMezuniyyetService
        : IServiceAsync<Mezuniyyet, MezuniyyetDto, MezuniyyetCreateDto, MezuniyyetUpdateDto>
    {
        // Mövcud metodlar
        Task<Result<IList<MezuniyyetListDto>>> GetListAsync();
        Task<Result> SobeReisiTesdiqAsync(int id, bool status, string? qeyd, int sobeReisiId);
        Task<Result> RehberTesdiqAsync(int id, bool status, string? qeyd, int rehberId);
        Task<Result> HrTesdiqAsync(int id, bool status, string? qeyd, int hrId);

        // YENİ: İşçi paneli üçün əlavə edildi
        Task<Result<IList<MezuniyyetListDto>>> GetIsciMezuniyyetleriAsync(int isciId);
        Task<Result> LegvEtAsync(int id, int isciId);

        // Təsdiq paneli üçün
        Task<Result<IList<MezuniyyetListDto>>> GetGozlemededeAsync();
        Task<Result<IList<MezuniyyetListDto>>> GetRehberTesdiqindeAsync();
        Task<Result<IList<MezuniyyetListDto>>> GetHrTesdiqindeAsync();
        // HR izləmə üçün — SobeReisi və Rəhbər təsdiqində dayanan müraciətlər
        Task<Result<IList<MezuniyyetListDto>>> GetProsesdeOlanlarAsync();

        /// <summary>
        /// Hazırda məzuniyyətdə olan (Təsdiqlənib + BaslamaTarixi ≤ bugün ≤ BitmeTarixi)
        /// və/və ya yaxın günlərdə başlayacaq işçilərin izləmə siyahısı.
        /// </summary>
        /// <param name="qabaqcaGun">Bugündən sonra neçə gün irəli (default 30) — "yaxın 30 gündə başlayacaqlar" üçün</param>
        Task<Result<IList<MezuniyyetListDto>>> GetAktivVeYaxinlardakilarAsync(int qabaqcaGun = 30);

        Task<Result<IList<MezuniyyetListDto>>> GetSobeyeGoreMezuniyyetlerAsync(int departamentId, int sobeReisiIsciId);
        Task<Result<IList<MezuniyyetListDto>>> GetFiltrliAsync( DateTime? baslaTarixFrom,DateTime? baslaTarixTo,int? departamentId,int? status,string? axtaris);

        // Təsdiq zamanı məlumatlandırma — paralel məzuniyyətlər + əvəzedici konflikti
        Task<Result<IList<MezuniyyetOverlapDto>>> GetOverlapMezuniyyetlerAsync(int mezuniyyetId, StrukturRolTipi? viewerRol = null);
        Task<Result<IList<EvezediciKonfliktDto>>> GetEvezediciKonfliktiAsync(int mezuniyyetId);

        // HR tərəfindən keçmiş tarixlər üçün məzuniyyətin geriyə qeyd edilməsi.
        // Təsdiq axınını atlayır, avtomatik təsdiqlənib statusunda yaranır,
        // davamiyyətdə "Qayib" qeydlərini "İcazəli"-yə çevirir.
        Task<Result<MezuniyyetDto>> GeriyeQeydEtAsync(GeriyeMezuniyyetCreateDto dto, int hrIsciId);
    }
}