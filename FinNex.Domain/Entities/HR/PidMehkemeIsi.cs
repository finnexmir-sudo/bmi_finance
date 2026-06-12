namespace FinNex.Domain.Entities.HR
{
    // PID (Problemli İşlər Departamenti) — məhkəmə işi.
    // Köhnə Excel "Məhkəmə 2021-2026" sheet-inə uyğun. Təkrarlanan iclas
    // sütunları ayrıca PidMehkemeIclas cədvəlinə çıxarılır (1-çox).
    public class PidMehkemeIsi : BaseEntity
    {
        public int? Sira { get; set; }                          // Excel sıra №
        public string? Status { get; set; }                     // BORC BAĞLANDI, davam edir, ...
        public string BorcluAd { get; set; } = "";              // borclu ad-soyad
        public string? KreditNovu { get; set; }                 // Daşınmaz / AVTOMOBİL ...
        // Müştəri açarı: hesab + subkod (gələcəkdə Oracle/başqa data çəkmək üçün əsas açar)
        public string? KreditHesabi { get; set; }               // hesab №
        public string? Subkod { get; set; }                     // subkod
        public DateTime? MehkemeyeVerilmeTarixi { get; set; }
        public string? MehkemeSenedi { get; set; }              // sənəd № + hakim adı
        public DateTime? QetnameTarixi { get; set; }
        public string? Qeyd { get; set; }

        // Opsional inteqrasiya (sonradan mövcud müştəri/kreditə bağlamaq üçün)
        public int? MusteriId { get; set; }
        public int? KreditId { get; set; }

        public ICollection<PidMehkemeIclas> Iclaslar { get; set; } = new List<PidMehkemeIclas>();
    }
}
