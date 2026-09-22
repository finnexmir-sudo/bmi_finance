using FinNex.Application.DTOs.Aml;
using FinNex.Application.DTOs.Risk;

namespace FinNex.Application.Interfaces.Aml;

/// <summary>«Məlumat Bazası» — dövr + siyahı üzrə AML paketi (BMI: <c>melumat_bazasi_kodlari.prg</c>).</summary>
public interface IMelumatBazasiService
{
    /// <summary>Vərəqlərin tərkibi (ad, başlıq, sorğu adı) — səhifədə siyahı göstərmək üçün.</summary>
    IReadOnlyList<MelumatBazasiVereqDto> Vereqler { get; }

    /// <summary>
    /// Dövr və axtarılanlar siyahısı üzrə 11 sorğunu icra edib paketi qaytarır.
    /// Oracle-a yalnız SELECT gedir. Bir vərəq alınmasa paket dayanmır —
    /// həmin vərəqin `Xeta`-sı doldurulur.
    /// </summary>
    /// <param name="axtarilanlar">
    /// Exceldən oxunan şəxslər. <b>BOŞ OLA BİLMƏZ</b> — siyahı olmadan sorğular
    /// bütün dövrü qaytarardı (BMI-də bu, `odb.aml_yoxlama` cədvəli idi).
    /// </param>
    /// <param name="yalnizFinVoen">
    /// `true` — yalnız FİN/VÖEN üzrə axtarılır, ad şərti söndürülür.
    /// SQL mətni dəyişmir; `a_s_a` xanasına tutmayan sentinel göndərilir.
    /// </param>
    Task<MelumatBazasiNeticeDto> HazirlaAsync(
        DateTime basTarix,
        DateTime sonTarix,
        IReadOnlyList<AxtarisSetriDto> axtarilanlar,
        bool yalnizFinVoen = false,
        CancellationToken ct = default);
}
