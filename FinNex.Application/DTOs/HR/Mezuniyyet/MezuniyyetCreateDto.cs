using FinNex.Domain.Entities.HR;

namespace FinNex.Application.DTOs.HR.Mezuniyyet
{
    // Yeni müraciət üçün
    public class MezuniyyetCreateDto
    {
        public int IsciId { get; set; } // Adətən sistem daxil olan işçini avtomatik götürür
        public int? EvezEdenIsciId { get; set; } // Bankda mütləqdir
        public MezuniyyetNovu Nov { get; set; }
        public DateTime BaslamaTarixi { get; set; }
        public DateTime BitmeTarixi { get; set; }
        public string? Qeyd { get; set; }

        // Məzuniyyət ödənişinin vaxtı (işçi seçir — default AySonuOdenis)
        public MezuniyyetOdenisTipi OdenisTipi { get; set; } = MezuniyyetOdenisTipi.AySonuOdenis;

        // Müraciət formasında ("Preview" endpoint-indən) son göstərilən "umumiNet" —
        // işçinin ekranında bir anlıq gördüyü təxmini net maaş. Burdan entity-yə
        // dondurularaq yazılır (14.10.2026). JS doldurur, boş qala bilər (məs.
        // "Öz hesabına" növündə preview heç göstərilmir).
        public decimal? TexminiNetMaas { get; set; }

        // Rol əsaslı workflow — controller tərəfindən doldurulur
        public bool MuracietSahibiRehberdirmi { get; set; }
        public bool MuracietSahibiSobeReisidirmi { get; set; }
        public bool MuracietSahibiHrdirmi { get; set; }
    }
}
