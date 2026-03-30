using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.Maas;

namespace FinNex.Application.Interfaces.Maas_If
{
    public interface IMaasHesablamaService
    {
        Task<Result<MaasHesablaNeticesiDto>> FerdiHesablaAsync(FerdiHesablaInputDto input);
        Task<Result<TopluHesablamaNeticesiDto>> TopluHesablaAsync(TopluHesablaInputDto input);

        /// <summary>
        /// Hemin aya dusen tesdiqli mezuniyyet is gunlerini sayir.
        /// Unpaid leave gunleri ayrica izlenilir -- buraya daxil deyil.
        /// </summary>
        Task<int> MezuniyyetGunleriniSayAsync(int isciId, int il, int ay);

        /// <summary>
        /// 2026 qaydasi:
        /// 1. Son 12 ayin yalniz islenmis (BrutMebleg > 0) aylari goturulur
        /// 2. avgDaily = totalBrut / islenmis_ay_sayi / 30
        /// 3. minDaily = cariMaas / 30
        /// 4. Netice = Max(avgDaily, minDaily) x gunSayi
        /// </summary>
        Task<decimal> MezuniyyetOdenisiniHesablaAsync(int isciId, int il, int ay, int gunSayi);
    }
}