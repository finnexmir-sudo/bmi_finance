namespace FinNex.Domain.Entities.Avtopark
{
    /// <summary>
    /// Müddət növü — icbari sığorta, kasko, texniki baxış, yağ dəyişmə…
    ///
    /// ENUM DEYİL, CƏDVƏLDİR. Səbəb: hər idarədə siyahı fərqlidir və yeni növ
    /// («təkər», «əyləc», «yanğınsöndürən») əlavə etmək üçün kod dəyişikliyi və
    /// yeni deploy tələb olunmamalıdır — admin özü idarə edir.
    /// </summary>
    public class MasinMuddetNovu : BaseEntity
    {
        public string Ad { get; set; } = null!;

        /// <summary>
        /// Bu növ üçün standart xəbərdarlıq müddəti (gün). Yeni müddət qeydi
        /// yaradılanda forma bu dəyərlə açılır; sətirdə əl ilə dəyişdirilə bilir.
        /// </summary>
        public int XeberdarliqGun { get; set; } = 30;

        /// <summary>Passiv növ yeni qeyd formasında görünmür, köhnə sətirlər qalır.</summary>
        public bool Aktivdir { get; set; } = true;

        /// <summary>Siyahıda göstərmə sırası (kiçik nömrə əvvəl).</summary>
        public int Sira { get; set; } = 0;
    }
}
