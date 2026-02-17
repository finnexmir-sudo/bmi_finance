using FinNex.Domain.Entities.SenedDovriyyesi;

namespace FinNex.Application.Struct
{
    public class SenedWorkflowService
    {
        public bool CanChangeStatus(
            string role,
            SenedStatusu current,
            SenedStatusu next)
        {
            if (role == "Admin" || role == "Manager")
                return true;

            if (role == "Operator")
            {
                // Operator yalnız Yeni -> Yoxlanilir edə bilər
                return current == SenedStatusu.Yeni
                       && next == SenedStatusu.Yoxlanilir;
            }

            if (role == "SobeRehberi")
            {
                if (current == SenedStatusu.Yoxlanilir &&
                    (next == SenedStatusu.Tesdiq ||
                     next == SenedStatusu.Yeni)) // Reject
                    return true;
            }

            return false;
        }

        public bool RequiresRejectComment(
            SenedStatusu current,
            SenedStatusu next)
        {
            return current == SenedStatusu.Yoxlanilir
                   && next == SenedStatusu.Yeni;
        }
    }

}
