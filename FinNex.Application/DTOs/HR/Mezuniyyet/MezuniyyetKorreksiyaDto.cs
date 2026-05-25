using Microsoft.AspNetCore.Http;

namespace FinNex.Application.DTOs.HR.Mezuniyyet;

/// <summary>
/// HR-in dövlət vəzifəsi korreksiyası üçün DTO.
/// İşçi illik məzuniyyətdə olarkən hərbi çağırış / məhkəmə şahidliyi kimi
/// dövlət vəzifəsi yerindyə yetirirsə, HR bu DTO ilə:
///   — o günləri balansdan geri qaytarır (+KorreksiyaGunSayi)
///   — həmin günlər üçün ayrıca DovletVezifelerininIcrasi qeydi yaradır
///   — maaş modulına MaasdanKes=false işarəsini göndərir
/// </summary>
public class MezuniyyetKorreksiyaDto
{
    /// <summary>Düzəliş edilecək əsas illik məzuniyyətin ID-si.</summary>
    public int MezuniyyetId { get; set; }

    /// <summary>İşçinin ID-si (double-check üçün).</summary>
    public int IsciId { get; set; }

    /// <summary>Dövlət vəzifəsinin başladığı tarix (əsas məzuniyyət içindəki gün).</summary>
    public DateTime BaslamaTarixi { get; set; }

    /// <summary>Dövlət vəzifəsinin bitdiyi tarix.</summary>
    public DateTime BitmeTarixi { get; set; }

    /// <summary>
    /// Korreksiya olunacaq gün sayı.
    /// Validation: > 0, əsas məzuniyyətin EfektivGunSayi-dan çox ola bilməz.
    /// </summary>
    public int KorreksiyaGunSayi { get; set; }

    /// <summary>HR-in açıqlaması (məs: "Hərbi komisarlıq çağırışı, əmr №123").</summary>
    public string? Qeyd { get; set; }

    /// <summary>
    /// Rəsmi sənədlər (hərbi əmr, məhkəmə vərəqəsi və s.) skanları.
    /// Opsional, çoxlu fayl göndərmək mümkündür.
    /// Sonradan da əlavə etmək olar.
    /// </summary>
    public List<IFormFile>? Senedler { get; set; }
}
