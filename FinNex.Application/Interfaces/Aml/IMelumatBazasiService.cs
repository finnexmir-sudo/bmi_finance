using FinNex.Application.DTOs.Aml;
using FinNex.Application.DTOs.Risk;

namespace FinNex.Application.Interfaces.Aml;

/// <summary>«Məlumat Bazası» — dövr + siyahı üzrə AML paketi (BMI: <c>melumat_bazasi_kodlari.prg</c>).</summary>
public interface IMelumatBazasiService
{
    /// <summary>Vərəqlərin tərkibi (ad, başlıq, sorğu adı) — səhifədə siyahı göstərmək üçün.</summary>
    IReadOnlyList<MelumatBazasiVereqDto> Vereqler { get; }

    /// <summary>
    /// Ümumilikdə axtarıla bilən maksimum şəxs sayı (bir neçə {SIYAHI} batch-inə
    /// bölünərək) — yükləmə ekranındakı «yalnız ilk N sətir axtarılacaq»
    /// xəbərdarlığı bu ədədi göstərsin deyə (`BmiLatin.MaxSetir`-dən FƏRQLİDİR —
    /// o, TƏK batch-in ölçüsüdür).
    /// </summary>
    int MaxUmumiSetir { get; }

    /// <summary>
    /// Dövr və axtarılanlar siyahısı üzrə 11 sorğunu icra edib paketi qaytarır.
    /// Oracle-a yalnız SELECT gedir. Bir vərəq alınmasa paket dayanmır —
    /// həmin vərəqin `Xeta`-sı doldurulur.
    /// </summary>
    /// <param name="axtarilanlar">
    /// Exceldən oxunan şəxslər. <b>BOŞ OLA BİLMƏZ</b> — siyahı olmadan sorğular
    /// bütün dövrü qaytarardı (BMI-də bu, `odb.aml_yoxlama` cədvəli idi).
    /// </param>
    /// <param name="rejim">
    /// Ad / FinVoen / HerIkisi (default) — hansı sütunlar uyğunluq üçün nəzərə
    /// alınsın. SQL mətni dəyişmir; nəzərə alınmayan xanaya tutmayan sentinel
    /// (`a_s_a` üçün `AdYoxdur`, FİN/VÖEN üçün `Bos`) göndərilir.
    /// </param>
    Task<MelumatBazasiNeticeDto> HazirlaAsync(
        DateTime basTarix,
        DateTime sonTarix,
        IReadOnlyList<AxtarisSetriDto> axtarilanlar,
        AxtarisRejimi rejim = AxtarisRejimi.HerIkisi,
        CancellationToken ct = default);
}
