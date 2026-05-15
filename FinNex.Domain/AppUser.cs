using FinNex.Domain.Entities.HR;
using FinNex.Domain.Entities.Structure;
using Microsoft.AspNetCore.Identity;

namespace FinNex.Domain
{
    public class AppUser : IdentityUser<int>
    {
        public string Ad { get; set; } = null!;
        public string Soyad { get; set; } = null!;

        // İstifadəçinin profil şəkli və ya qeydiyyat tarixi kimi əlavə sütunlar
        public DateTime QeydiyyatTarixi { get; set; } = DateTime.Now;
        public bool Aktivdir { get; set; } = true;
        public ICollection<UserDepartment> UserDepartments { get; set; }
    = new List<UserDepartment>();

        public int? IsciId { get; set; }
        public Isci? Isci { get; set; }

        // Mail cavabı üçün SMTP məlumatları (şifrəli saxlanır)
        public string? MailSmtpHost { get; set; }
        public string? MailSmtpEmail { get; set; }
        public string? MailSmtpParol { get; set; }
    }
}
