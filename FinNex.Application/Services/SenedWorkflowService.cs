using FinNex.Application.Interfaces;
using FinNex.Domain.Entities.SenedDovriyyesi;

public class SenedWorkflowService : ISenedWorkflowService
{
    public bool CanChangeStatus(string role, SenedStatusu current, SenedStatusu next)
    {
        if (role == "Admin")
            return true;

        if (role == "Operator")
            return false;

        if (role is "Manager" or "SobeRehberi")
        {
            if (current == SenedStatusu.Yeni && next == SenedStatusu.Yoxlanilir)
                return true;

            if (current == SenedStatusu.Yoxlanilir &&
                (next == SenedStatusu.Tesdiq || next == SenedStatusu.Yeni))
                return true;

            if (current == SenedStatusu.Tesdiq && next == SenedStatusu.Arxiv)
                return true;
        }

        return false;
    }

    public bool CanEdit(string role, SenedStatusu status)
    {
        if (role == "Admin")
            return true;

        if (role == "Operator" && status == SenedStatusu.Yeni)
            return true;

        if (role is "Manager" or "SobeRehberi")
            return status is SenedStatusu.Yeni or SenedStatusu.Yoxlanilir;

        return false;
    }

    public bool CanDelete(string role)
    {
        return role is "Admin" or "Manager" or "SobeRehberi";
    }

    public bool RequiresRejectComment(SenedStatusu current, SenedStatusu next)
    {
        return current == SenedStatusu.Yoxlanilir && next == SenedStatusu.Yeni;
    }
}
