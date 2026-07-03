namespace FinNex.Application.DTOs.HR.Mezuniyyet
{
    /// <summary>
    /// Bir işçinin illik məzuniyyət hüququnun AVTOMATİK hesablanması (yalnız oxuma — yoxlama üçün).
    /// Düstur: əsas(21/30/42) + staj(0/2/4/6, əlildə yox — M.116.3) + uşaq(0/2/5).
    /// </summary>
    public class MezuniyyetHuquqDto
    {
        public int    IsciId       { get; set; }
        public string IsciAdSoyad  { get; set; } = "";
        public string SobeAdi      { get; set; } = "-";
        public string VezifeAdi    { get; set; } = "-";

        public bool Elildir  { get; set; }
        public int  VezifeGun { get; set; }   // vəzifədən (21/30); 0 = təyin olunmayıb → 21 kimi işlənir
        public int  EsasGun   { get; set; }   // 21/30/42 (əlil override)

        public double StajIl  { get; set; }   // ümumi staj (il)
        public int    StajGun { get; set; }   // 0/2/4/6 (əlildə 0)

        public int  UsaqSayi       { get; set; }
        public bool EngelliUsaqVar { get; set; }
        public int  UsaqGun        { get; set; }   // 0/2/5

        public int IllikHuquq { get; set; }   // əsas + staj + uşaq

        public DateTime IsheQebulTarixi { get; set; }

        // İzahat üçün — hesablamanın necə alındığını göstərmək
        public string Izah =>
            $"{EsasGun}" +
            (StajGun > 0 ? $" + {StajGun} (staj)" : "") +
            (UsaqGun > 0 ? $" + {UsaqGun} (uşaq)" : "");
    }
}
