using FinNex.Domain.Entities.Structure;


namespace FinNex.Domain.Entities.SenedDovriyyesi
{
    public class SenedDovriyyesiIstifadeciIcazesi: BaseEntity
    {
        public int Id { get; set; }

        public int IstifadeciId { get; set; }
        public AppUser? Istifadeci { get; set; }

        public int SobeId { get; set; }
        public Departament? Departament { get; set; }

        public IcazeNovu IcazeNovu { get; set; }
    }
    public enum IcazeNovu
    {
        View = 1,
        Full = 2
    }
}
