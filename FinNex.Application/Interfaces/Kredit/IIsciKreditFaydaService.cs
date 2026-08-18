using FinNex.Application.DTOs.Kredit;

namespace FinNex.Application.Interfaces.Kredit;

/// <summary>
/// VM 98.2.1 — işçi kreditləri üzrə hesabi gəlir.
///
/// DİQQƏT: imza dəyişəndə İNTERFEYS + İMPLEMENTASİYA + BÜTÜN ÇAĞIRIŞ YERLƏRİ
/// eyni anda yenilənməlidir (CLAUDE.md — CS0535/CS1501 tələsi).
/// </summary>
public interface IIsciKreditFaydaService
{
    /// <summary>
    /// Verilmiş dövr üçün hesablayır. Oracle-a YALNIZ SELECT gedir.
    /// Xəta halında istisna ATMIR — nəticənin <c>Xeta</c> sahəsi dolur ki,
    /// maaş səhifəsi açılmaqdan qalmasın.
    /// </summary>
    Task<IsciKreditFaydaNeticeDto> HesablaAsync(DateTime bas, DateTime son, CancellationToken ct = default);

    /// <summary>
    /// Dövrün özünü təyin edir: BAS = sonuncu ödənilmiş maaşın tarixi,
    /// SON = cari gün − 1. Tapılmasa BAS null qaytarılır (mühasib özü yazsın).
    /// </summary>
    Task<(DateTime? Bas, DateTime Son)> DovrTeklifAsync();
}
