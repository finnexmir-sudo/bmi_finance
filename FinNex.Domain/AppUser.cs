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
    }
}
