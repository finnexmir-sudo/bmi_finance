namespace FinNex.Domain.Entities.HR
{
    public enum IcazeStatus
    {
        Gozlemede = 1,  // İlkin müraciət
        SobeReisiTesdiqinde = 2,  // Şöbə rəisi baxır
        RehberTesdiqinde = 3,  // Rəhbər baxır
        HrTesdiqinde = 4,  // HR son təsdiq
        Tesdiqlenib = 5,  // Uğurla bitdi
        ImtinaEdildi = 6   // Rədd edildi
    }
}