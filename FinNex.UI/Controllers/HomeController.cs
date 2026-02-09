using FinNex.UI.Models;
using FinNex.UI.ViewModels.Dashboard;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace FinNex.UI.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            var vm = new DashboardVM
            {
                UmumiLayiheler = 12,
                AktivModullar = 4,
                IstifadeciSayi = 150,
                SistemStatusu = "Online",
                Alerts = new List<DashboardAlertVM>
            {
                new() { Type="warning", Text="3 bank məlumatı natamamdır (VÖEN/Kod boşdur)." },
                new() { Type="danger", Text="1 əməliyyat uğursuz oldu – loglara baxın." },
                new() { Type="info", Text="5 əməliyyat gözləmədədir (təsdiq gözləyir)." },
            },
                RecentTransactions = new List<RecentTransactionVM>
            {
                new() { Tarix = DateTime.Today.AddMinutes(-15), Bank="Kapital Bank", Mebleg=1200, Valyuta="AZN", Status="Tamamlandı" },
                new() { Tarix = DateTime.Today.AddMinutes(-40), Bank="ABB", Mebleg=500, Valyuta="AZN", Status="Gözləmədə" },
                new() { Tarix = DateTime.Today.AddHours(-2), Bank="PAŞA Bank", Mebleg=9800, Valyuta="AZN", Status="Tamamlandı" },
                new() { Tarix = DateTime.Today.AddHours(-3), Bank="Yelo Bank", Mebleg=250, Valyuta="AZN", Status="Xəta" },
                new() { Tarix = DateTime.Today.AddHours(-6), Bank="Unibank", Mebleg=410, Valyuta="AZN", Status="Tamamlandı" },
            },
                MonthlyLabels = new() { "Sən", "Okt", "Noy", "Dek", "Yan", "Fev" },
                MonthlyAmounts = new() { 12000, 14500, 9800, 18000, 21000, 19500 },

                Last7DaysLabels = new() { "B.e", "Ç.a", "Ç", "C.a", "C", "Ş", "B" },
                Last7DaysCounts = new() { 12, 19, 14, 22, 30, 17, 25 }
            };

            return View(vm);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
