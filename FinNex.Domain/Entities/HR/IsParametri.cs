namespace FinNex.Domain.Entities.HR
{
    public class IsParametri : BaseEntity
    {
        public TimeSpan StandartGirisVaxti { get; set; } = new TimeSpan(9, 0, 0);
        public TimeSpan StandartCixisVaxti { get; set; } = new TimeSpan(17, 45, 0);
        public int GecikmeToleransDeqiqe { get; set; } = 5;
        public int TezCixmaToleransDeqiqe { get; set; } = 15;
    }
}
