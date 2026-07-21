namespace FinNex.Application.DTOs.Muhasibat;

// IFRS 9 ECL hesablama parametrləri — floor (aşağı hədd) dərəcələri.
// DMS-də JSON kimi saxlanılır (DB migration lazım deyil); default = cari dəyərlər.
// DİQQƏT: bu, birbaşa ehtiyat hesablamasına təsir edir — dəyişikdən sonra real data ilə yoxla.
public class Ifrs9ParametrDto
{
    // Mənzil güzəşti açıqdırsa, mənzil (1902/1904) krediti aşağı floor (MenzilFloor) alır.
    // Bağlıdırsa — mənzil də adi kredit kimi mərhələ floor-una (Stage1/Stage2) tabe olur.
    public bool MenzilGuzest { get; set; } = true;

    public decimal MenzilFloor { get; set; } = 0.1m;   // mənzil floor, % (məs. 0.1)
    public decimal Stage1Floor { get; set; } = 1.0m;   // Mərhələ 1 floor, %
    public decimal Stage2Floor { get; set; } = 2.0m;   // Mərhələ 2 floor, %
}
