-- =========================================================================
-- ENTERPRISE DYNAMIC WORKFLOW ENGINE DATABASE SCHEMA SCRIPT
-- Target Database: SQL Server 2016+
-- =========================================================================

-- Create Database statement (Uncomment to execute if starting fresh)
-- CREATE DATABASE WorkflowEngineDb;
-- GO
-- USE WorkflowEngineDb;
-- GO

-- =========================================================================
-- 1. ORGANIZATIONAL STRUCTURE TABLES
-- =========================================================================

CREATE TABLE Companies (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(250) NOT NULL,
    Code NVARCHAR(50) NOT NULL UNIQUE,
    Description NVARCHAR(MAX) NULL,
    CreatedBy NVARCHAR(100) NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy NVARCHAR(100) NULL,
    ModifiedDate DATETIME2 NULL,
    DeletedBy NVARCHAR(100) NULL,
    DeletedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    RowVersion ROWVERSION
);

CREATE TABLE Branches (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    CompanyId UNIQUEIDENTIFIER NOT NULL,
    Name NVARCHAR(250) NOT NULL,
    Code NVARCHAR(50) NOT NULL UNIQUE,
    CreatedBy NVARCHAR(100) NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy NVARCHAR(100) NULL,
    ModifiedDate DATETIME2 NULL,
    DeletedBy NVARCHAR(100) NULL,
    DeletedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    RowVersion ROWVERSION,
    CONSTRAINT FK_Branches_Companies FOREIGN KEY (CompanyId) REFERENCES Companies(Id)
);

CREATE TABLE Departments (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(250) NOT NULL,
    Code NVARCHAR(50) NOT NULL UNIQUE,
    CreatedBy NVARCHAR(100) NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy NVARCHAR(100) NULL,
    ModifiedDate DATETIME2 NULL,
    DeletedBy NVARCHAR(100) NULL,
    DeletedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    RowVersion ROWVERSION
);

CREATE TABLE Designations (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(250) NOT NULL,
    Code NVARCHAR(50) NOT NULL UNIQUE,
    CreatedBy NVARCHAR(100) NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy NVARCHAR(100) NULL,
    ModifiedDate DATETIME2 NULL,
    DeletedBy NVARCHAR(100) NULL,
    DeletedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    RowVersion ROWVERSION
);

CREATE TABLE Roles (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(250) NOT NULL,
    Code NVARCHAR(50) NOT NULL UNIQUE,
    Description NVARCHAR(MAX) NULL,
    CreatedBy NVARCHAR(100) NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy NVARCHAR(100) NULL,
    ModifiedDate DATETIME2 NULL,
    DeletedBy NVARCHAR(100) NULL,
    DeletedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    RowVersion ROWVERSION
);

CREATE TABLE Users (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Username NVARCHAR(100) NOT NULL UNIQUE,
    Email NVARCHAR(250) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(500) NOT NULL,
    FullName NVARCHAR(250) NOT NULL,
    CompanyId UNIQUEIDENTIFIER NULL,
    BranchId UNIQUEIDENTIFIER NULL,
    DepartmentId UNIQUEIDENTIFIER NULL,
    DesignationId UNIQUEIDENTIFIER NULL,
    RoleId UNIQUEIDENTIFIER NULL,
    ManagerId UNIQUEIDENTIFIER NULL,
    RefreshToken NVARCHAR(500) NULL,
    RefreshTokenExpiryTime DATETIME2 NULL,
    CreatedBy NVARCHAR(100) NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy NVARCHAR(100) NULL,
    ModifiedDate DATETIME2 NULL,
    DeletedBy NVARCHAR(100) NULL,
    DeletedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    RowVersion ROWVERSION,
    CONSTRAINT FK_Users_Companies FOREIGN KEY (CompanyId) REFERENCES Companies(Id) ON DELETE SET NULL,
    CONSTRAINT FK_Users_Branches FOREIGN KEY (BranchId) REFERENCES Branches(Id) ON DELETE SET NULL,
    CONSTRAINT FK_Users_Departments FOREIGN KEY (DepartmentId) REFERENCES Departments(Id) ON DELETE SET NULL,
    CONSTRAINT FK_Users_Designations FOREIGN KEY (DesignationId) REFERENCES Designations(Id) ON DELETE SET NULL,
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES Roles(Id) ON DELETE SET NULL,
    CONSTRAINT FK_Users_Users FOREIGN KEY (ManagerId) REFERENCES Users(Id) ON DELETE NO ACTION
);

-- =========================================================================
-- 2. WORKFLOW SCHEMA CONFIGURATIONS
-- =========================================================================

CREATE TABLE WorkflowDefinitions (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(250) NOT NULL,
    Code NVARCHAR(50) NOT NULL UNIQUE,
    Description NVARCHAR(MAX) NULL,
    Module NVARCHAR(100) NULL,
    Category NVARCHAR(100) NULL,
    CreatedBy NVARCHAR(100) NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy NVARCHAR(100) NULL,
    ModifiedDate DATETIME2 NULL,
    DeletedBy NVARCHAR(100) NULL,
    DeletedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    RowVersion ROWVERSION
);

CREATE TABLE WorkflowVersions (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    WorkflowDefinitionId UNIQUEIDENTIFIER NOT NULL,
    VersionNumber INT NOT NULL,
    Status INT NOT NULL DEFAULT 1, -- 1: Draft, 2: Published, 3: Deactivated
    EffectiveDate DATETIME2 NULL,
    ExpiryDate DATETIME2 NULL,
    Priority INT NOT NULL DEFAULT 1,
    CreatedBy NVARCHAR(100) NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy NVARCHAR(100) NULL,
    ModifiedDate DATETIME2 NULL,
    DeletedBy NVARCHAR(100) NULL,
    DeletedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    RowVersion ROWVERSION,
    CONSTRAINT FK_WorkflowVersions_Definitions FOREIGN KEY (WorkflowDefinitionId) REFERENCES WorkflowDefinitions(Id)
);

CREATE TABLE WorkflowSteps (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    WorkflowVersionId UNIQUEIDENTIFIER NOT NULL,
    StepNumber INT NOT NULL,
    Name NVARCHAR(250) NOT NULL,
    ApprovalType INT NOT NULL, -- Sequential, Parallel, AnyOne, Majority, DeptHead etc.
    Sequence INT NOT NULL,
    IsMandatory BIT NOT NULL DEFAULT 1,
    ParallelApproval BIT NOT NULL DEFAULT 0,
    SLAHours INT NOT NULL DEFAULT 24,
    EmailNotification BIT NOT NULL DEFAULT 1,
    SMSNotification BIT NOT NULL DEFAULT 0,
    PushNotification BIT NOT NULL DEFAULT 0,
    AutoApproval BIT NOT NULL DEFAULT 0,
    RejectAllowed BIT NOT NULL DEFAULT 1,
    ReturnAllowed BIT NOT NULL DEFAULT 1,
    DelegateAllowed BIT NOT NULL DEFAULT 1,
    CommentsMandatory BIT NOT NULL DEFAULT 0,
    AttachmentMandatory BIT NOT NULL DEFAULT 0,
    DigitalSignatureRequired BIT NOT NULL DEFAULT 0,
    TargetRoleId NVARCHAR(150) NULL,
    TargetUserId NVARCHAR(150) NULL,
    TargetDepartmentId NVARCHAR(150) NULL,
    ApiLookupUrl NVARCHAR(500) NULL,
    CreatedBy NVARCHAR(100) NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy NVARCHAR(100) NULL,
    ModifiedDate DATETIME2 NULL,
    DeletedBy NVARCHAR(100) NULL,
    DeletedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    RowVersion ROWVERSION,
    CONSTRAINT FK_WorkflowSteps_Versions FOREIGN KEY (WorkflowVersionId) REFERENCES WorkflowVersions(Id)
);

CREATE TABLE WorkflowTransitions (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    WorkflowVersionId UNIQUEIDENTIFIER NOT NULL,
    FromStepId UNIQUEIDENTIFIER NOT NULL,
    ToStepId UNIQUEIDENTIFIER NOT NULL,
    Description NVARCHAR(MAX) NULL,
    CreatedBy NVARCHAR(100) NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy NVARCHAR(100) NULL,
    ModifiedDate DATETIME2 NULL,
    DeletedBy NVARCHAR(100) NULL,
    DeletedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    RowVersion ROWVERSION,
    CONSTRAINT FK_WorkflowTransitions_Versions FOREIGN KEY (WorkflowVersionId) REFERENCES WorkflowVersions(Id),
    CONSTRAINT FK_WorkflowTransitions_FromSteps FOREIGN KEY (FromStepId) REFERENCES WorkflowSteps(Id) ON DELETE NO ACTION,
    CONSTRAINT FK_WorkflowTransitions_ToSteps FOREIGN KEY (ToStepId) REFERENCES WorkflowSteps(Id) ON DELETE NO ACTION
);

CREATE TABLE WorkflowConditionGroups (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    WorkflowStepId UNIQUEIDENTIFIER NULL,
    WorkflowTransitionId UNIQUEIDENTIFIER NULL,
    LogicalOperator INT NOT NULL DEFAULT 1, -- AND, OR, NOT
    CreatedBy NVARCHAR(100) NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy NVARCHAR(100) NULL,
    ModifiedDate DATETIME2 NULL,
    DeletedBy NVARCHAR(100) NULL,
    DeletedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    RowVersion ROWVERSION,
    CONSTRAINT FK_ConditionGroups_Steps FOREIGN KEY (WorkflowStepId) REFERENCES WorkflowSteps(Id) ON DELETE SET NULL,
    CONSTRAINT FK_ConditionGroups_Transitions FOREIGN KEY (WorkflowTransitionId) REFERENCES WorkflowTransitions(Id) ON DELETE SET NULL
);

CREATE TABLE WorkflowConditions (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ConditionGroupId UNIQUEIDENTIFIER NOT NULL,
    FieldName NVARCHAR(150) NOT NULL,
    Operator INT NOT NULL, -- Equals, NotEquals, GreaterThan, LessThan etc.
    Value NVARCHAR(500) NOT NULL,
    CreatedBy NVARCHAR(100) NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy NVARCHAR(100) NULL,
    ModifiedDate DATETIME2 NULL,
    DeletedBy NVARCHAR(100) NULL,
    DeletedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    RowVersion ROWVERSION,
    CONSTRAINT FK_WorkflowConditions_Groups FOREIGN KEY (ConditionGroupId) REFERENCES WorkflowConditionGroups(Id)
);

CREATE TABLE WorkflowApprovers (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    WorkflowStepId UNIQUEIDENTIFIER NOT NULL,
    UserId UNIQUEIDENTIFIER NOT NULL,
    CreatedBy NVARCHAR(100) NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy NVARCHAR(100) NULL,
    ModifiedDate DATETIME2 NULL,
    DeletedBy NVARCHAR(100) NULL,
    DeletedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    RowVersion ROWVERSION,
    CONSTRAINT FK_WorkflowApprovers_Steps FOREIGN KEY (WorkflowStepId) REFERENCES WorkflowSteps(Id),
    CONSTRAINT FK_WorkflowApprovers_Users FOREIGN KEY (UserId) REFERENCES Users(Id)
);

CREATE TABLE WorkflowEscalations (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    WorkflowStepId UNIQUEIDENTIFIER NOT NULL,
    TimeoutHours INT NOT NULL DEFAULT 24,
    Action INT NOT NULL DEFAULT 1, -- Reminder, EscalateToManager, AutoApprove, AutoReject etc.
    EscalateToUserId UNIQUEIDENTIFIER NULL,
    IsProcessed BIT NOT NULL DEFAULT 0,
    CreatedBy NVARCHAR(100) NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy NVARCHAR(100) NULL,
    ModifiedDate DATETIME2 NULL,
    DeletedBy NVARCHAR(100) NULL,
    DeletedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    RowVersion ROWVERSION,
    CONSTRAINT FK_WorkflowEscalations_Steps FOREIGN KEY (WorkflowStepId) REFERENCES WorkflowSteps(Id),
    CONSTRAINT FK_WorkflowEscalations_Users FOREIGN KEY (EscalateToUserId) REFERENCES Users(Id) ON DELETE SET NULL
);

-- =========================================================================
-- 3. DYNAMIC FORM ENGINE SCHEMA TABLES
-- =========================================================================

CREATE TABLE WorkflowForms (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    WorkflowVersionId UNIQUEIDENTIFIER NOT NULL,
    Name NVARCHAR(250) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    CreatedBy NVARCHAR(100) NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy NVARCHAR(100) NULL,
    ModifiedDate DATETIME2 NULL,
    DeletedBy NVARCHAR(100) NULL,
    DeletedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    RowVersion ROWVERSION,
    CONSTRAINT FK_WorkflowForms_Versions FOREIGN KEY (WorkflowVersionId) REFERENCES WorkflowVersions(Id)
);

CREATE TABLE WorkflowFields (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    FormId UNIQUEIDENTIFIER NOT NULL,
    Name NVARCHAR(150) NOT NULL,
    Label NVARCHAR(250) NOT NULL,
    Type INT NOT NULL, -- TextBox, DropDown, Date, Number, TextArea etc.
    Placeholder NVARCHAR(250) NULL,
    DefaultValue NVARCHAR(500) NULL,
    ValidationRegex NVARCHAR(500) NULL,
    IsRequired BIT NOT NULL DEFAULT 0,
    IsReadOnly BIT NOT NULL DEFAULT 0,
    [Order] INT NOT NULL DEFAULT 0,
    GridColumns NVARCHAR(MAX) NULL,
    CreatedBy NVARCHAR(100) NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy NVARCHAR(100) NULL,
    ModifiedDate DATETIME2 NULL,
    DeletedBy NVARCHAR(100) NULL,
    DeletedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    RowVersion ROWVERSION,
    CONSTRAINT FK_WorkflowFields_Forms FOREIGN KEY (FormId) REFERENCES WorkflowForms(Id)
);

CREATE TABLE WorkflowFieldOptions (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    FieldId UNIQUEIDENTIFIER NOT NULL,
    Label NVARCHAR(250) NOT NULL,
    Value NVARCHAR(500) NOT NULL,
    CreatedBy NVARCHAR(100) NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy NVARCHAR(100) NULL,
    ModifiedDate DATETIME2 NULL,
    DeletedBy NVARCHAR(100) NULL,
    DeletedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    RowVersion ROWVERSION,
    CONSTRAINT FK_FieldOptions_Fields FOREIGN KEY (FieldId) REFERENCES WorkflowFields(Id)
);

-- =========================================================================
-- 4. RUNTIME EXECUTION INSTANCES & TRANSITIONS LOGS
-- =========================================================================

CREATE TABLE WorkflowInstances (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    WorkflowVersionId UNIQUEIDENTIFIER NOT NULL,
    InstanceNumber NVARCHAR(100) NOT NULL UNIQUE,
    Status INT NOT NULL DEFAULT 1, -- InProgress, Approved, Rejected, Returned, Cancelled
    RequesterId UNIQUEIDENTIFIER NOT NULL,
    StartDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    EndDate DATETIME2 NULL,
    CreatedBy NVARCHAR(100) NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy NVARCHAR(100) NULL,
    ModifiedDate DATETIME2 NULL,
    DeletedBy NVARCHAR(100) NULL,
    DeletedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    RowVersion ROWVERSION,
    CONSTRAINT FK_WorkflowInstances_Versions FOREIGN KEY (WorkflowVersionId) REFERENCES WorkflowVersions(Id),
    CONSTRAINT FK_WorkflowInstances_Requester FOREIGN KEY (RequesterId) REFERENCES Users(Id) ON DELETE NO ACTION
);

CREATE TABLE WorkflowInstanceData (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    WorkflowInstanceId UNIQUEIDENTIFIER NOT NULL,
    FieldName NVARCHAR(150) NOT NULL,
    FieldValue NVARCHAR(MAX) NULL,
    CreatedBy NVARCHAR(100) NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy NVARCHAR(100) NULL,
    ModifiedDate DATETIME2 NULL,
    DeletedBy NVARCHAR(100) NULL,
    DeletedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    RowVersion ROWVERSION,
    CONSTRAINT FK_InstanceData_Instances FOREIGN KEY (WorkflowInstanceId) REFERENCES WorkflowInstances(Id)
);

CREATE TABLE WorkflowInstanceSteps (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    WorkflowInstanceId UNIQUEIDENTIFIER NOT NULL,
    WorkflowStepId UNIQUEIDENTIFIER NOT NULL,
    StepNumber INT NOT NULL,
    Name NVARCHAR(250) NOT NULL,
    Status INT NOT NULL DEFAULT 1, -- Pending, Approved, Rejected, Returned etc.
    StartDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    EndDate DATETIME2 NULL,
    DueDate DATETIME2 NULL,
    CreatedBy NVARCHAR(100) NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy NVARCHAR(100) NULL,
    ModifiedDate DATETIME2 NULL,
    DeletedBy NVARCHAR(100) NULL,
    DeletedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    RowVersion ROWVERSION,
    CONSTRAINT FK_InstanceSteps_Instances FOREIGN KEY (WorkflowInstanceId) REFERENCES WorkflowInstances(Id),
    CONSTRAINT FK_InstanceSteps_Steps FOREIGN KEY (WorkflowStepId) REFERENCES WorkflowSteps(Id)
);

CREATE TABLE WorkflowApprovals (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    WorkflowInstanceStepId UNIQUEIDENTIFIER NOT NULL,
    ApproverId UNIQUEIDENTIFIER NOT NULL,
    Status INT NOT NULL DEFAULT 1, -- Pending, Approved, Rejected etc.
    ActionDate DATETIME2 NULL,
    Comment NVARCHAR(MAX) NULL,
    DigitalSignature NVARCHAR(500) NULL,
    DelegatedFromUserId UNIQUEIDENTIFIER NULL,
    CreatedBy NVARCHAR(100) NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy NVARCHAR(100) NULL,
    ModifiedDate DATETIME2 NULL,
    DeletedBy NVARCHAR(100) NULL,
    DeletedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    RowVersion ROWVERSION,
    CONSTRAINT FK_Approvals_Steps FOREIGN KEY (WorkflowInstanceStepId) REFERENCES WorkflowInstanceSteps(Id),
    CONSTRAINT FK_Approvals_Approvers FOREIGN KEY (ApproverId) REFERENCES Users(Id) ON DELETE NO ACTION,
    CONSTRAINT FK_Approvals_DelegatedUsers FOREIGN KEY (DelegatedFromUserId) REFERENCES Users(Id) ON DELETE NO ACTION
);

CREATE TABLE WorkflowHistory (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    WorkflowInstanceId UNIQUEIDENTIFIER NOT NULL,
    StepName NVARCHAR(250) NOT NULL,
    Action NVARCHAR(100) NOT NULL, -- Started, Approved, Rejected, Returned, Delegated etc.
    PerformedById UNIQUEIDENTIFIER NOT NULL,
    ActionDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    Comment NVARCHAR(MAX) NULL,
    CreatedBy NVARCHAR(100) NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy NVARCHAR(100) NULL,
    ModifiedDate DATETIME2 NULL,
    DeletedBy NVARCHAR(100) NULL,
    DeletedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    RowVersion ROWVERSION,
    CONSTRAINT FK_WorkflowHistory_Instances FOREIGN KEY (WorkflowInstanceId) REFERENCES WorkflowInstances(Id),
    CONSTRAINT FK_WorkflowHistory_Users FOREIGN KEY (PerformedById) REFERENCES Users(Id)
);

CREATE TABLE WorkflowComments (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    WorkflowInstanceId UNIQUEIDENTIFIER NOT NULL,
    WorkflowStepId UNIQUEIDENTIFIER NULL,
    UserId UNIQUEIDENTIFIER NOT NULL,
    Content NVARCHAR(MAX) NOT NULL,
    CreatedBy NVARCHAR(100) NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy NVARCHAR(100) NULL,
    ModifiedDate DATETIME2 NULL,
    DeletedBy NVARCHAR(100) NULL,
    DeletedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    RowVersion ROWVERSION,
    CONSTRAINT FK_Comments_Instances FOREIGN KEY (WorkflowInstanceId) REFERENCES WorkflowInstances(Id),
    CONSTRAINT FK_Comments_Steps FOREIGN KEY (WorkflowStepId) REFERENCES WorkflowSteps(Id) ON DELETE SET NULL,
    CONSTRAINT FK_Comments_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE NO ACTION
);

CREATE TABLE WorkflowAttachments (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    WorkflowInstanceId UNIQUEIDENTIFIER NOT NULL,
    WorkflowStepId UNIQUEIDENTIFIER NULL,
    FileName NVARCHAR(250) NOT NULL,
    FilePath NVARCHAR(500) NOT NULL,
    FileSize BIGINT NOT NULL,
    UploadedById UNIQUEIDENTIFIER NOT NULL,
    CreatedBy NVARCHAR(100) NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy NVARCHAR(100) NULL,
    ModifiedDate DATETIME2 NULL,
    DeletedBy NVARCHAR(100) NULL,
    DeletedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    RowVersion ROWVERSION,
    CONSTRAINT FK_Attachments_Instances FOREIGN KEY (WorkflowInstanceId) REFERENCES WorkflowInstances(Id),
    CONSTRAINT FK_Attachments_Steps FOREIGN KEY (WorkflowStepId) REFERENCES WorkflowSteps(Id) ON DELETE SET NULL,
    CONSTRAINT FK_Attachments_Users FOREIGN KEY (UploadedById) REFERENCES Users(Id) ON DELETE NO ACTION
);

-- =========================================================================
-- 5. NOTIFICATION SYSTEMS AND SECURITY SCHEMAS
-- =========================================================================

CREATE TABLE NotificationTemplates (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(250) NOT NULL,
    Code NVARCHAR(50) NOT NULL UNIQUE,
    Subject NVARCHAR(250) NOT NULL,
    Body NVARCHAR(MAX) NOT NULL,
    Type NVARCHAR(50) NOT NULL DEFAULT 'Email',
    CreatedBy NVARCHAR(100) NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy NVARCHAR(100) NULL,
    ModifiedDate DATETIME2 NULL,
    DeletedBy NVARCHAR(100) NULL,
    DeletedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    RowVersion ROWVERSION
);

CREATE TABLE EmailQueue (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    RecipientEmail NVARCHAR(250) NOT NULL,
    Subject NVARCHAR(250) NOT NULL,
    Body NVARCHAR(MAX) NOT NULL,
    IsSent BIT NOT NULL DEFAULT 0,
    RetryCount INT NOT NULL DEFAULT 0,
    ErrorMessage NVARCHAR(MAX) NULL,
    SentDate DATETIME2 NULL,
    CreatedBy NVARCHAR(100) NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy NVARCHAR(100) NULL,
    ModifiedDate DATETIME2 NULL,
    DeletedBy NVARCHAR(100) NULL,
    DeletedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    RowVersion ROWVERSION
);

CREATE TABLE SMSQueue (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    RecipientPhone NVARCHAR(50) NOT NULL,
    Message NVARCHAR(MAX) NOT NULL,
    IsSent BIT NOT NULL DEFAULT 0,
    RetryCount INT NOT NULL DEFAULT 0,
    ErrorMessage NVARCHAR(MAX) NULL,
    SentDate DATETIME2 NULL,
    CreatedBy NVARCHAR(100) NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy NVARCHAR(100) NULL,
    ModifiedDate DATETIME2 NULL,
    DeletedBy NVARCHAR(100) NULL,
    DeletedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    RowVersion ROWVERSION
);

CREATE TABLE WebhookQueue (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    TargetUrl NVARCHAR(500) NOT NULL,
    Payload NVARCHAR(MAX) NOT NULL,
    IsSent BIT NOT NULL DEFAULT 0,
    RetryCount INT NOT NULL DEFAULT 0,
    ErrorMessage NVARCHAR(MAX) NULL,
    SentDate DATETIME2 NULL,
    CreatedBy NVARCHAR(100) NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy NVARCHAR(100) NULL,
    ModifiedDate DATETIME2 NULL,
    DeletedBy NVARCHAR(100) NULL,
    DeletedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    RowVersion ROWVERSION
);

CREATE TABLE SystemLogs (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    LogLevel NVARCHAR(50) NOT NULL DEFAULT 'Error',
    Message NVARCHAR(MAX) NOT NULL,
    Exception NVARCHAR(MAX) NULL,
    Source NVARCHAR(250) NULL,
    Timestamp DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CreatedBy NVARCHAR(100) NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy NVARCHAR(100) NULL,
    ModifiedDate DATETIME2 NULL,
    DeletedBy NVARCHAR(100) NULL,
    DeletedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    RowVersion ROWVERSION
);

-- =========================================================================
-- 6. INDEXES ON FREQUENTLY ACCESSED FOREIGN KEYS
-- =========================================================================

CREATE INDEX IX_Users_DepartmentId ON Users(DepartmentId);
CREATE INDEX IX_Users_RoleId ON Users(RoleId);
CREATE INDEX IX_Users_ManagerId ON Users(ManagerId);
CREATE INDEX IX_WorkflowSteps_VersionId ON WorkflowSteps(WorkflowVersionId);
CREATE INDEX IX_WorkflowTransitions_FromStep ON WorkflowTransitions(FromStepId);
CREATE INDEX IX_WorkflowTransitions_ToStep ON WorkflowTransitions(ToStepId);
CREATE INDEX IX_WorkflowInstances_VersionId ON WorkflowInstances(WorkflowVersionId);
CREATE INDEX IX_WorkflowInstances_RequesterId ON WorkflowInstances(RequesterId);
CREATE INDEX IX_WorkflowInstanceSteps_InstanceId ON WorkflowInstanceSteps(WorkflowInstanceId);
CREATE INDEX IX_WorkflowApprovals_StepId ON WorkflowApprovals(WorkflowInstanceStepId);
CREATE INDEX IX_WorkflowApprovals_ApproverId ON WorkflowApprovals(ApproverId);
CREATE INDEX IX_EmailQueue_IsSent ON EmailQueue(IsSent);
