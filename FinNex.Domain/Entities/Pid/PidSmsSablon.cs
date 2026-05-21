using FinNex.Domain.Entities.HR;

namespace FinNex.Domain.Entities.Pid;

/// <summary>
/// PİD (Problemli İşlər Departamenti) tərəfindən göndərilən SMS şablonları.
/// Operator şablonu seçib parametrləri doldurur — mətn şablondan generasiya olunur.
/// Şablon mətnində kişiselləşdirmə üçün Göyərçin formatı istifadə oluna bilər:
///     "Dear {{1}}, your debt is {{2}} AZN."
/// </summary>
public class PidSmsSablon : BaseEntity
{
    public string Ad { get; set; } = null!;            // şablonun daxili adı (məs. "Borc xatırlatması")
    public string Metn { get; set; } = null!;          // şablon mətni (English)
    public string? Aciqlama { get; set; }              // operator üçün qısa izah
    public bool Aktiv { get; set; } = true;
}
