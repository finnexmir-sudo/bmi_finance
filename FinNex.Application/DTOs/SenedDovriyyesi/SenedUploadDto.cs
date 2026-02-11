using Microsoft.AspNetCore.Http;

namespace FinNex.Application.DTOs.SenedDovriyyesi
{
    public class SenedUploadDto
    {
        public int SobeId { get; set; }
        public int SenedNovuId { get; set; }
        public string Basliq { get; set; } = null!;
        public string AcarSoz { get; set; } = null!;
        public IFormFile Fayl { get; set; } = null!;
    }

}
