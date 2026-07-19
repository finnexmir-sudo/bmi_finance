namespace FinNex.UI.Areas.Admin.ViewModels
{
    // Sistem İcazələri — icazə kataloqu + istifadəçi təyinatı (general, hər sahə üçün).
    public class SistemIcazeIndexVM
    {
        public List<PermissionSatirVM> Icazeler { get; set; } = new();
    }

    public class PermissionSatirVM
    {
        public int Id { get; set; }
        public string Kod { get; set; } = "";
        public string Ad { get; set; } = "";
        public string? Aciqlama { get; set; }
        public int UserSayi { get; set; }
    }

    public class SistemIcazeIdareVM
    {
        public int PermissionId { get; set; }
        public string PermissionKod { get; set; } = "";
        public string PermissionAd { get; set; } = "";
        public List<IcazeUserVM> Users { get; set; } = new();
    }

    public class IcazeUserVM
    {
        public int UserId { get; set; }
        public string AdSoyad { get; set; } = "";
        public string UserName { get; set; } = "";
        public IList<string> Roller { get; set; } = new List<string>();
        public bool Var { get; set; }
    }
}
