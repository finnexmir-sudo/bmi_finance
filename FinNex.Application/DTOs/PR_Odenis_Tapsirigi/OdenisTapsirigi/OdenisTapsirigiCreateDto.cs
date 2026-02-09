
namespace FinNex.Application.DTOs.PR_Odenis_Tapsirigi.OdenisTapsirigi
{
    public class OdenisTapsirigiCreateDto
    {
        public DateTime Tarix { get; set; }

        public int OduyenMusteriId { get; set; }
        public int OduyenHesabId { get; set; }

        public int AlanMusteriId { get; set; }
        public int AlanHesabId { get; set; }

        public decimal Mebleg { get; set; }
        public string Valyuta { get; set; } = null!;
        public string Teyinat { get; set; } = null!;
    }
    public class OdenisTapsirigiListDto : BaseDto
    {
        public string Nomre { get; set; } = null!;
        public DateTime Tarix { get; set; }
        public decimal Mebleg { get; set; }
        public string Valyuta { get; set; } = null!;
    }
    public class OdenisTapsirigiUpdateDto : BaseDto
    {
        public DateTime Tarix { get; set; }

        public int OduyenMusteriId { get; set; }
        public int OduyenHesabId { get; set; }

        public int AlanMusteriId { get; set; }
        public int AlanHesabId { get; set; }

        public decimal Mebleg { get; set; }
        public string Valyuta { get; set; } = null!;
        public string Teyinat { get; set; } = null!;
    }
    public class OdenisTapsirigiDetailDto : BaseDto
    {
        public string Nomre { get; set; } = null!;
        public DateTime Tarix { get; set; }

        public decimal Mebleg { get; set; }
        public string Valyuta { get; set; } = null!;
        public string Teyinat { get; set; } = null!;

        public string OduyenMusteriAdi { get; set; } = null!;
        public string OduyenHesabIban { get; set; } = null!;

        public string AlanMusteriAdi { get; set; } = null!;
        public string AlanHesabIban { get; set; } = null!;
    }
}
