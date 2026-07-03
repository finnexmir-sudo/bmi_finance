using FinNex.Domain.Entities.Structure;

namespace FinNex.Domain.Entities.HR
{
    public class Vezife : BaseEntity
    {
        public string Ad { get; set; } = null!;
        public string? Tesvir { get; set; }
        public bool Aktivdir { get; set; } = true;

        /// <summary>
        /// Bu vəzifə üçün əsas əmək məzuniyyətinin təqvim günü (Əmək Məcəlləsi 114):
        /// 21 — adi işçi, 30 — mütəxəssis/rəhbər. Əlil işçi üçün balans hesablamasında
        /// 42-yə (M.119) keçir — bu, vəzifədən asılı deyil, işçinin əlillik faktından gəlir.
        /// </summary>
        public int EsasMezuniyyetGunu { get; set; } = 21;

        public int DepartamentId { get; set; }
        public Departament Departament { get; set; } = null!;

        public ICollection<IsciTeyinat> IsciTeyinatlar { get; set; }
            = new List<IsciTeyinat>();
    }

}
