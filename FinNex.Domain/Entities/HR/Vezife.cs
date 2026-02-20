using FinNex.Domain.Entities.Structure;

namespace FinNex.Domain.Entities.HR
{
    public class Vezife : BaseEntity
    {
        public string Ad { get; set; } = null!;
        public string? Tesvir { get; set; }
        public bool Aktivdir { get; set; } = true;

        // --- ƏLAVƏ EDİLMƏLİ OLAN HİSSƏ ---
        public int DepartamentId { get; set; } // Foreign Key
        public Department Departament { get; set; } = null!; // Navigation Property
                                                              // --------------------------------

        public ICollection<Isci> Isciler { get; set; } = new List<Isci>();
    }

}
