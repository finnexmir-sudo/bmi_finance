using FinNex.Application.Interfaces.SenedDovriyyesi;
using FinNex.UI.ViewModels.SenedDovriyyesi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.User.SenedDovriyyesi.Controllers;

[Area("SenedDovriyyesi")]
[Authorize]
public class DashboardController : Controller
{
    private readonly ISenedService _senedService;

    public DashboardController(ISenedService senedService)
    {
        _senedService = senedService;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Sənəd Dövriyyəsi";
        ViewData["ActivePage"] = "Dashboard";

        var result = await _senedService.GetDashboardAsync();

        var vm = new SenedDashboardVM();

        if (result.Success && result.Data != null)
        {
            vm.UmumiSenedler = result.Data.UmumiSenedler;
            vm.YeniSenedler = result.Data.YeniSenedler;
            vm.YoxlanilirSenedler = result.Data.YoxlanilirSenedler;
            vm.TesdiqSenedler = result.Data.TesdiqSenedler;
            vm.ArxivSenedler = result.Data.ArxivSenedler;
            vm.SonSenedler = result.Data.SonSenedler.Select(s => new SenedDashboardItemVM
            {
                Id = s.Id,
                Basliq = s.Basliq,
                Departament = s.Sobe,
                SenedNovu = s.SenedNovu,
                Status = s.Status,
                FaylSayi = s.FaylSayi,
                YaradilmaTarixi = s.YaradilmaTarixi
            }).ToList();
        }

        return View(vm);
    }
}
