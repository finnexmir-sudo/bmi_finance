
namespace FinNex.Application.DTOs.PR_Odenis_Tapsirigi.MusteriHesabi
{
    public class MusteriHesabiListDto : BaseDto
    {
        public string Iban { get; set; } = null!;
        public string Valyuta { get; set; } = null!;
    }
    public class MusteriHesabiUpdateDto : BaseDto
    {
        public string Iban { get; set; } = null!;
        public string Valyuta { get; set; } = null!;
    }
    public class MusteriHesabiCreateDto
    {
        public int MusteriId { get; set; }

        public string Iban { get; set; } = null!;
        public string Valyuta { get; set; } = null!;
    }
    public class MusteriHesabiDetailDto : BaseDto
    {
        public string Iban { get; set; } = null!;
        public string Valyuta { get; set; } = null!;

        // İstəyə görə: müştəri adı
        public string MusteriAdi { get; set; } = null!;
    }
}
