using FinNex.Application.DTOs.Kredit.Arayis;

namespace FinNex.Application.Interfaces.Kredit;

/// <summary>
/// Kredit arayışları üçün Oracle axtarışları — YALNIZ SELECT (02.09.2026).
///
/// BMI-də bu sorğular `frmborcalantemizlik` və `zaminarayis` formalarının
/// içində, TextBox mətni birbaşa SQL-ə yapışdırılmaqla qurulurdu (inyeksiyaya
/// açıq). Burada SQL Admin → Oracle Sorğular-da saxlanılır, axtarış dəyəri isə
/// servisdə TƏMİZLƏNİR (yalnız rəqəm/hərf) — mətn birbaşa SQL-ə düşmür.
///
/// ⚠️ Bu servis HEÇ NƏ YAZMIR. Məktub jurnalına yazı `IXaricMektubService`
/// vasitəsilə controller-də olur — Oracle-a yazı isə ümumiyyətlə qadağandır.
/// </summary>
public interface IKreditArayisService
{
    /// <summary>
    /// Borcalanın kreditləri — QEYDİYYAT KODU (regnom) üzrə.
    /// Sorğu adı: «Arayış Borcalan» (Admin → Oracle Sorğular, aktiv olmalıdır).
    /// </summary>
    Task<List<BorcalanArayisSatirDto>> BorcalanAxtarAsync(string regnom, CancellationToken ct = default);

    /// <summary>
    /// Zaminin zaminlikləri — ZAMİNİN FİN kodu üzrə.
    /// Sorğu adı: «Arayış Zamin».
    /// </summary>
    Task<List<ZaminArayisSatirDto>> ZaminAxtarAsync(string pincode, CancellationToken ct = default);
}
