using FinNex.Domain.Entities.SenedDovriyyesi;

public interface ISenedWorkflowService
{
    bool CanChangeStatus(string role, SenedStatusu current, SenedStatusu next);

    bool CanEdit(string role, SenedStatusu status);

    bool CanDelete(string role);

    bool RequiresRejectComment(SenedStatusu current, SenedStatusu next);
}
