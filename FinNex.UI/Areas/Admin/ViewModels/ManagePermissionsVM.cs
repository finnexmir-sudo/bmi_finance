namespace FinNex.UI.Areas.Admin.ViewModels
{
    public class ManagePermissionsVM
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = "";
        public string UserName { get; set; } = "";

        public List<DepartmentPermissionRow> Rows { get; set; } = new();
    }
    public class DepartmentPermissionRow
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = "";

        // null = icazə yoxdur, 1 = View, 2 = Full
        public int? CurrentPermission { get; set; }
        public int ExistingRecordId { get; set; } // mövcud icazənin Id-si (update/sil üçün)
    }

    public class SavePermissionsVM
    {
        public int UserId { get; set; }
        // key = DepartmentId, value = 0 (yoxdur), 1 (View), 2 (Full)
        public Dictionary<int, int> Permissions { get; set; } = new();
    }
}
