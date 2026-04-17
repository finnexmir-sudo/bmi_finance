namespace FinNex.Application.DTOs.HR.Mezuniyyet
{
    /// <summary>
    /// Müraciət edən işçinin eyni dövrdə başqa işçi üçün qəbul edilmiş əvəzedici
    /// olduğunu göstərən xəbərdarlıq. Soft-warn — təsdiqçi qərar verir.
    /// </summary>
    public class EvezediciKonfliktDto
    {
        public int MezuniyyetId { get; set; }
        public int MuracietEdenIsciId { get; set; }
        public string MuracietEdenIsciAdSoyad { get; set; } = "";
        public DateTime BaslamaTarixi { get; set; }
        public DateTime BitmeTarixi { get; set; }
    }
}
