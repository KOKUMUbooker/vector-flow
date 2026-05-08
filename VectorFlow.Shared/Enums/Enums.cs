namespace VectorFlow.Shared.Enums;

public enum WorkspaceRole
{
    Owner,
    Admin,
    Member
}

public enum ActivityAction
{
    StatusChanged,
    PriorityChanged,
    AssigneeChanged,
    TitleChanged,
    LabelAdded,
    LabelRemoved,
    DueDateChanged
}

public enum InvitationStatus
{
    Pending,
    Accepted,
    Declined,
    Expired
}