using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Emeliyyat;

namespace FinNex.Application.Interfaces.Emeliyyat;

/// <summary>
/// 20 000 USD limitini aşan köçürmənin əsaslandırma sənədi — növlər siyahısı.
///
/// Siyahı BOŞ başlayır (seed yoxdur, uydurma ad yazılmayıb). Operator lazım
/// olan növü elə köçürmə formasından əlavə edir; növbəti dəfə hamı siyahıdan
/// seçir — «hərə nə gəldi yazmasın» qaydası belə qorunur.
/// </summary>
public interface IKocurmeSenedNovuService
{
    /// <summary>Seçim siyahısı — yalnız aktiv növlər, sıra ilə.</summary>
    Task<IList<KocurmeSenedNovuDto>> AktivlerAsync();

    /// <summary>Admin siyahısı — deaktivlər də daxil.</summary>
    Task<IList<KocurmeSenedNovuDto>> HamisiAsync();

    /// <summary>
    /// Yeni növ. Eyni adlı AKTİV növ varsa yenisi yaradılmır — mövcudun Id-si
    /// qaytarılır (operatorlar eyni şeyi fərqli vaxtlarda əlavə edir).
    /// Deaktiv edilmiş eyni adlı növ varsa YENİDƏN AKTİVLƏŞDİRİLİR — dublikat
    /// sətir yaratmaqdansa köhnəni qaytarmaq tarixçəni bütöv saxlayır.
    /// </summary>
    Task<Result<int>> YaratAsync(string ad, int userId);

    /// <summary>Aktiv/deaktiv keçidi. Növ SİLİNMİR — köçürmələr ona bağlıdır.</summary>
    Task<Result> VeziyyetDeyisAsync(int id, int userId);
}
