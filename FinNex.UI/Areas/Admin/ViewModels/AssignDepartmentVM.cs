using Microsoft.AspNetCore.Mvc.Rendering;

namespace FinNex.UI.Areas.Admin.ViewModels
{
    public class AssignDepartmentVM
    {
        public int UserId { get; set; }

        public int DepartmentId { get; set; }

        public List<SelectListItem> Departments { get; set; }
            = new();
    }
}
