namespace FinNex.UI.ViewModels.Dashboard;

public class DashboardVM
{
    public int UmumiLayiheler { get; set; }
    public int AktivModullar { get; set; }
    public int IstifadeciSayi { get; set; }
    public string SistemStatusu { get; set; } = "Online";

    public List<DashboardAlertVM> Alerts { get; set; } = new();
    public List<RecentTransactionVM> RecentTransactions { get; set; } = new();

    // Charts
    public List<string> MonthlyLabels { get; set; } = new();
    public List<decimal> MonthlyAmounts { get; set; } = new();

    public List<string> Last7DaysLabels { get; set; } = new();
    public List<int> Last7DaysCounts { get; set; } = new();
}

public class DashboardAlertVM
{
    public string Type { get; set; } = "warning"; // success, warning, danger, info
    public string Text { get; set; } = "";
}

public class RecentTransactionVM
{
    public DateTime Tarix { get; set; }
    public string Bank { get; set; } = "";
    public decimal Mebleg { get; set; }
    public string Valyuta { get; set; } = "AZN";
    public string Status { get; set; } = "Tamamlandı"; // Tamamlandı, Gözləmədə, Xəta
}
