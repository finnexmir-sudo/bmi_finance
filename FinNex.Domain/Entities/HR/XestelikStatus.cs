namespace FinNex.Domain.Entities.HR
{
    public enum XestelikStatus
    {
        /// <summary>HR yaratdığı zaman avtomatik təsdiqlənir.</summary>
        Tesdiqlenib = 1,

        /// <summary>HR sonradan ləğv edə bilər.</summary>
        LegvEdildi = 2
    }
}
