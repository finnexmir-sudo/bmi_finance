namespace FinNex.Domain.Entities.HR
{
    public enum MezuniyyetStatus
    {
        Gozlemede = 1,           // İlkin müraciət
        SobeReisiTesdiqinde = 2, // Şöbə rəisi baxır
        RehberTesdiqinde = 3,    // Rəhbər/Departament müdiri baxır
        HrTesdiqinde = 4,        // HR son sənədləşməni edir
        Tesdiqlenib = 5,         // Proses uğurla bitdi
        ImtinaEdildi = 6,        // Hər hansı mərhələdə rədd edildi
        LegvEdildi = 7           // İşçi tərəfindən ləğv edildi
    }

}
