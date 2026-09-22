using FinNex.Application.DTOs.Aml;

namespace FinNex.Application.Interfaces.Aml;

/// <summary>«Məlumat Bazası» — dövr üzrə AML paketi (BMI: <c>AML/Sorgular/MelumatBazasi.cs</c>).</summary>
public interface IMelumatBazasiService
{
    /// <summary>Vərəqlərin tərkibi (ad, başlıq, sorğu adı) — səhifədə siyahı göstərmək üçün.</summary>
    IReadOnlyList<MelumatBazasiVereqDto> Vereqler { get; }

    /// <summary>
    /// Dövr üzrə 11 sorğunu icra edib paketi qaytarır. Oracle-a yalnız SELECT gedir.
    /// Bir vərəq alınmasa paket dayanmır — həmin vərəqin `Xeta`-sı doldurulur.
    /// </summary>
    Task<MelumatBazasiNeticeDto> HazirlaAsync(DateTime basTarix, DateTime sonTarix, CancellationToken ct = default);
}
