using FinNex.Application.DTOs.SenedDovriyyesi.Sened;
using FinNex.Application.DTOs.SenedDovriyyesi.SenedNovu;
using FinNex.Application.Interfaces.SenedDovriyyesi;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.SenedDovriyyesi.Controllers
{
    [Area("SenedDovriyyesi")]
    public class SenedNovuController : Controller
    {
        private readonly ISenedNovuService _senedNovuService;

        public SenedNovuController(ISenedNovuService senedNovuService)
        {
            _senedNovuService = senedNovuService;
        }

        [HttpPost]
        public async Task<IActionResult> SenedNovuYarat([FromBody] SenedNovuCreateDto dto)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Məlumatlar tam deyil." });

            // Sənəd növünü yaradırıq
            var result = await _senedNovuService.AddAsync(dto);

            if (result.Success)
            {
                // Yeni yaranan növü və onun ID-sini qaytarırıq ki, dropdown-a əlavə edək
                return Json(new { success = true, id = result.Data, ad = dto.Ad });
            }

            return Json(new { success = false, message = result.Message });
        }
    }
}
