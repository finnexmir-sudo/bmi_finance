using FinNex.UI.Filters;

namespace FinNex.UI.Areas.Avtopark;

/// <summary>
/// Avtopark idarəetmə səhifələri (Maşınlar, Müddətlər) üçün giriş yoxlaması.
///
/// **Admin həmişə girir.** Ondan başqa — Admin panel → Sistem İcazələri
/// bölməsindən <see cref="IcazeKodu"/> icazəsi verilmiş istifadəçi.
///
/// NİYƏ ROL DEYİL: əvvəl bu iki səhifə `[Authorize(Roles = Admin)]` idi. Yəni
/// təsərrüfat işçisinə maşın kartını açmaq üçün TAM ADMİN vermək lazım gəlirdi —
/// o isə maaşdan tutmuş sistem ayarlarına qədər hər şeyi açır. İndi ayrıca
/// icazə var: yalnız Avtopark idarəetməsi açılır, qalan səlahiyyətlər dəyişmir.
///
/// BÜTÜN MƏNTİQ <see cref="IcazeAttribute"/>-dədir — bu sinif yalnız kodu
/// bir yerdə saxlayan «adlandırılmış qısaltmadır». Yeni səhifə üçün ayrıca
/// belə sinif yazmaq MƏCBURİ DEYİL, birbaşa `[Icaze("kod")]` yazmaq kifayətdir.
///
/// ⚠️ Sidebar-dakı şərt (`_UserLayout.cshtml` → `hasAvtoparkIdare`) BUNUNLA
/// EYNİ olmalıdır — linki görüb sonra 403 almaq istifadəçini çaşdırır.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class AvtoparkIdareIcazesiAttribute : IcazeAttribute
{
    /// <summary>
    /// İcazə kodu — `Permissions` cədvəlindəki `Kod` sütunu ilə HƏRFƏN eynidir.
    /// Adı `Kod` DEYİL, çünki bazada `IcazeAttribute.Kod` xassəsi var və eyni
    /// adlı sabit onu kölgələyərdi (`new` tələb edərdi) — oxuyan çaşardı.
    /// </summary>
    public const string IcazeKodu = "avtopark_idare";

    public AvtoparkIdareIcazesiAttribute() : base(IcazeKodu) { }
}
