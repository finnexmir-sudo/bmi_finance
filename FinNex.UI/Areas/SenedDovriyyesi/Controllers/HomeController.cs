using FinNex.Application.DTOs.SenedDovriyyesi.Sened;
using FinNex.Application.Interfaces.SenedDovriyyesi;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.SenedDovriyyesi.Controllers
{
    [Area("SenedDovriyyesi")]
    public class HomeController : Controller
    {
        private readonly ISenedNovuService _senedNovuService;

        public HomeController(ISenedNovuService senedNovuService)
        {
            _senedNovuService = senedNovuService;
        }

       
    }
}
