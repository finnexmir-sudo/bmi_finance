using FinNex.Domain.Entities.HR;

namespace FinNex.Application.Common.Extensions
{
    /// <summary>
    /// Məzuniyyət növünün istifadəçiyə göstərilən Azərbaycan adı — TƏK MƏNBƏ.
    /// Əvvəllər bu switch 6+ yerdə təkrarlanırdı və hər biri fərqli növləri
    /// buraxırdı (məs. OzHesabina heç birində, DovletVezifesi yalnız birində) —
    /// nəticədə xam enum adı ("OzHesabina") görünürdü. Yeni növ əlavə edəndə
    /// yalnız BURANI yeniləmək kifayətdir.
    ///
    /// DİQQƏT: İllik/Xəstəlik/Ezamiyyət mətnləri məzuniyyət siyahısındakı filtr
    /// (Index.cshtml) dəyərləri ilə EYNİ olmalıdır — dəyişmə, yoxsa filtr sınar.
    /// </summary>
    public static class MezuniyyetNovuExtensions
    {
        public static string Adi(this MezuniyyetNovu nov) => nov switch
        {
            MezuniyyetNovu.Illik => "Əmək məzuniyyəti",
            MezuniyyetNovu.Xestelik => "Xəstəlik məzuniyyəti",
            MezuniyyetNovu.Ezamiyyet => "Ezamiyyət",
            MezuniyyetNovu.DovletVezifelerininIcrasi => "Dövlət Vəzifəsi",
            MezuniyyetNovu.OzHesabina => "Öz hesabına (ödənişsiz)",
            _ => nov.ToString()
        };
    }
}
