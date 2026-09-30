namespace FinNex.Application.DTOs.HR.Mezuniyyet
{
    /// <summary>
    /// Əmək məzuniyyəti hüququ hesablamasının qanuni ədədləri — HR-in
    /// "HR → Məzuniyyət Balansı → Hüquq parametrləri" səhifəsindən idarə
    /// etdiyi TƏK sətir. Sahələr <c>MezuniyyetHuquqParametrleri</c> entitisi
    /// ilə BİRƏ-BİR eynidir (oxuma/yazma DTO-su birdir, sahə sayı azdır).
    /// </summary>
    public class MezuniyyetHuquqParametrleriDto
    {
        public int EsasGunAdi { get; set; } = 21;
        public int EsasGunElil { get; set; } = 42;

        // ⚠️ `decimal` — `double` YOX. Bax entity-dəki eyni qeyd (az-AZ
        // mədəniyyəti + FlexibleDecimalModelBinder yalnız decimal-ı tutur).
        public decimal StajHedd1Il { get; set; } = 5;
        public int StajHedd1Gun { get; set; } = 2;
        public decimal StajHedd2Il { get; set; } = 10;
        public int StajHedd2Gun { get; set; } = 4;
        public decimal StajHedd3Il { get; set; } = 15;
        public int StajHedd3Gun { get; set; } = 6;

        public int UsaqYasHeddi { get; set; } = 14;
        public int EngelliUsaqYasHeddi { get; set; } = 18;
        public int UsaqSayi2GunHeddi { get; set; } = 2;
        public int UsaqGun2 { get; set; } = 2;
        public int UsaqSayi5GunHeddi { get; set; } = 3;
        public int UsaqGun5 { get; set; } = 5;
    }
}
