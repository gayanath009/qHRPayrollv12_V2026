namespace WorkflowEngine.Domain.Enums;

public enum WorkflowStatus
{
    Draft = 1,
    Published = 2,
    Deactivated = 3
}

public enum ApprovalType
{
    Sequential = 1,
    Parallel = 2,
    AnyOne = 3,
    AllMustApprove = 4,
    Majority = 5,
    Conditional = 6,
    DynamicUser = 7,
    ManagerHierarchy = 8,
    DepartmentHead = 9,
    RoleBased = 10,
    SpecificUser = 11,
    ApiLookup = 12
}

public enum StepStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Returned = 4,
    Escalated = 5,
    Delegated = 6,
    Skipped = 7
}

public enum NotificationType
{
    Email = 1,
    SMS = 2,
    MicrosoftTeams = 3,
    Slack = 4,
    WhatsApp = 5,
    PushNotification = 6,
    Webhook = 7
}

public enum FieldType
{
    TextBox = 1,
    DropDown = 2,
    Date = 3,
    Number = 4,
    CheckBox = 5,
    RadioButton = 6,
    TextArea = 7,
    Lookup = 8,
    Attachment = 9,
    Signature = 10,
    HiddenField = 11,
    CalculatedField = 12,
    DynamicGrid = 13
}

public enum WorkflowInstanceStatus
{
    InProgress = 1,
    Approved = 2,
    Rejected = 3,
    Returned = 4,
    Cancelled = 5
}

public enum ConditionOperator
{
    Equals = 1,
    NotEquals = 2,
    GreaterThan = 3,
    LessThan = 4,
    GreaterThanOrEqual = 5,
    LessThanOrEqual = 6,
    Contains = 7,
    StartsWith = 8,
    EndsWith = 9
}

public enum LogicalOperator
{
    AND = 1,
    OR = 2,
    NOT = 3
}

public enum EscalationAction
{
    Reminder = 1,
    EscalateToManager = 2,
    EscalateToNextLevel = 3,
    AutoApprove = 4,
    AutoReject = 5,
    NotifyAdmin = 6
}
