using FinNex.Application.DTOs.Aml;

namespace FinNex.Application.Interfaces.Aml;

public interface IAmlHesabatService
{
    /// <summary>
    /// AML → «Hesab üzrə sorğu» hesabatını icra edir (BMI `frmhesabsorgu` əvəzi).
    /// Oracle-a YALNIZ SELECT gedir.
    /// </summary>
    Task<AmlHesabNeticeDto> IcraEtAsync(AmlHesabSorguDto sorgu, CancellationToken ct = default);
}
