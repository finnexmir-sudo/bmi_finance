using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Paylasim;

namespace FinNex.Application.Interfaces.Paylasim
{
    /// <summary>
    /// "Paylaşımlar" — Admin-in xaricdən aldığı mənalı məlumatı rich-text (şəkilli)
    /// formatda işçilərlə bölüşdüyü bilik lenti (27.10.2026, pilot). Elan-dan AYRI
    /// — Elan əməliyyat xəbərdarlığıdır, bu sadəcə oxuma məqsədlidir.
    /// </summary>
    public interface IPaylasimService
    {
        /// <summary>Admin idarəetmə siyahısı — aktiv/deaktiv hamısı, ən yenidən.</summary>
        Task<List<PaylasimListDto>> HamisiniGetirAsync();

        /// <summary>İşçi lenti — yalnız aktiv VƏ bu işçiyə hədəflənmiş (yaxud "hamısına") postlar, ən yenidən.</summary>
        Task<List<PaylasimListDto>> AktivLentAsync(int isciId);

        Task<PaylasimListDto?> DetailGetirAsync(int id);

        /// <param name="appUserId">Göndərənin AppUser.Id-si (Isci DEYİL — admin hesabının işçi profili olmaya bilər).</param>
        Task<Result<int>> YaratAsync(PaylasimYaratDto dto, int appUserId);

        /// <param name="appUserId">"Son seçilmişlər" keşini bu adminin adına yeniləmək üçün.</param>
        Task<Result> YenileAsync(int id, PaylasimYaratDto dto, int appUserId);

        /// <summary>Göstər/gizlət — silmədən lentdən çıxarır (geri qaytarıla bilər).</summary>
        Task<Result> AktivlikDeyisAsync(int id);

        Task<Result> SilAsync(int id);

        /// <summary>Hədəf-seçim siyahısı üçün aktiv işçilər (İşçi Sıralaması qaydası — Sira/Ad/Soyad).</summary>
        Task<List<PaylasimIsciSecimDto>> AktivIscilerAsync();

        /// <summary>Bu postun hədəflədiyi Isci.Id-lər (boşdursa post "Hamısına"dır).</summary>
        Task<List<int>> HedefIsciIdleriAsync(int melumatId);

        /// <summary>Bu adminin "Seçilmiş işçilər" rejimində SON istifadə etdiyi siyahı — Yarat ekranında əvvəlcədən işarələmək üçün.</summary>
        Task<List<int>> SonHedefKitlesiAsync(int adminAppUserId);

        /// <summary>Kateqoriya seçim siyahısı — yalnız aktiv, ada görə sıralanmış.</summary>
        Task<List<PaylasimKateqoriyaDto>> AktivKateqoriyalarAsync();

        /// <summary>Formadan "+ Yeni" ilə sürətli kateqoriya yaratma. Eyni adda aktiv kateqoriya varsa, onu qaytarır (dublikat yaratmır).</summary>
        Task<Result<PaylasimKateqoriyaDto>> KateqoriyaYaratAsync(string ad);

        /// <summary>Qaralamanı dərc edir — Qaralamadir=false, Aktivdir=true.</summary>
        Task<Result> PaylaAsync(int id);

        /// <summary>
        /// Bu işçiyə görünən postlardan, son baxışından sonra yaradılmışların sayı —
        /// sidebar-dakı "yeni paylaşım" nişanı üçün (27.10.2026).
        /// </summary>
        Task<int> YeniSayAsync(int isciId, int appUserId);

        /// <summary>İşçinin "son baxış" tarixini indiyə yeniləyir (/User/Paylasim açılanda çağırılır).</summary>
        Task GorulduIsaretleAsync(int appUserId);
    }
}
