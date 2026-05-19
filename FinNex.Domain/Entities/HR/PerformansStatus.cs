namespace FinNex.Domain.Entities.HR
{
    public enum PerformansStatus
    {
        Gozlemede       = 0,  // HR kampaniya başlatdı, işçi gözlənilir
        SobeReisiGozleyir = 1, // İşçi özünü qiymətləndirdi, şöbə rəisi gözlənilir
        RehberGozleyir  = 2,  // Şöbə rəisi qiymətləndirdi, rəhbər gözlənilir
        Rehber2Gozleyir = 3,  // Rehber1 qiymətləndirdi, Rehber2 gözlənilir
        Tamamlandi      = 4   // Proses tamamlandı
    }
}
