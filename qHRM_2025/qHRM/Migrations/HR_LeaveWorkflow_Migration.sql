-- =============================================================================
-- HR & Payroll – Leave Workflow Integration Migration Script
-- Target Database: qHRMasterV26
-- Version: 1.0.0
-- Date: 2026-08-19
-- Author: qHRM Integration
-- =============================================================================
-- SAFE TO RUN: This script only ADDS new tables/columns. No existing data 
-- is modified or deleted.
-- =============================================================================

USE qHRMasterV26;
GO

-- =============================================================================
-- 1. LEAVE WORKFLOW CONFIGURATION
--    Maps each Leave Type to a Dynamic Workflow Definition code.
--    Admins configure which workflow runs for which leave type.
-- =============================================================================
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'HrLvWfConfig')
BEGIN
    CREATE TABLE HrLvWfConfig (
        WfCfgId         INT IDENTITY(1,1)   NOT NULL PRIMARY KEY,
        LvTypId         INT                 NOT NULL,   -- FK → HR_LvType.LvTyp_Id
        WfCode          NVARCHAR(50)        NOT NULL,   -- Maps to WorkflowDefinition.Code in WF engine
        IsEnabled       BIT                 NOT NULL DEFAULT 1,
        -- Effective dates (optional, for future-dated configs)
        EffDate         DATE                NULL,
        ExpDate         DATE                NULL,
        -- Approver resolution config (JSON) e.g. {"HrManagerEmpId":5,"FinanceEmpId":12,"CeoEmpId":1}
        ApproverConfig  NVARCHAR(MAX)       NULL,
        -- Policy rules (JSON) e.g. {"MedCertRequired":true,"PayrollCutoffDay":20}
        PolicyRules     NVARCHAR(MAX)       NULL,
        -- Audit
        RecStat         SMALLINT            NOT NULL DEFAULT 1,
        CrtId           INT                 NOT NULL DEFAULT 0,
        CrtDateTime     DATETIME            NOT NULL DEFAULT GETDATE(),
        MdfId           INT                 NULL,
        MdfDateTime     DATETIME            NULL,

        CONSTRAINT FK_HrLvWfConfig_LvType FOREIGN KEY (LvTypId) REFERENCES HR_LvType(LvTyp_Id)
    );

    PRINT 'Table HrLvWfConfig created.';
END
ELSE
    PRINT 'Table HrLvWfConfig already exists – skipped.';
GO

-- =============================================================================
-- 2. LEAVE WORKFLOW INSTANCES
--    Tracks the Dynamic Workflow instance ID for each leave application.
-- =============================================================================
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'HrLvWfInstance')
BEGIN
    CREATE TABLE HrLvWfInstance (
        WfInstId        INT IDENTITY(1,1)   NOT NULL PRIMARY KEY,
        LvId            INT                 NOT NULL,   -- FK → HR_LvRegister.Lv_Id
        WfInstanceId    UNIQUEIDENTIFIER    NOT NULL,   -- WorkflowInstance.Id from WF Engine
        WfInstanceNo    NVARCHAR(100)       NOT NULL,   -- e.g. LV_SICK-20260819-4521
        WfStatus        NVARCHAR(30)        NOT NULL DEFAULT 'InProgress',
        -- InProgress | Approved | Rejected | Cancelled | Returned
        CrtDateTime     DATETIME            NOT NULL DEFAULT GETDATE(),
        MdfDateTime     DATETIME            NULL,

        CONSTRAINT FK_HrLvWfInstance_LvReg FOREIGN KEY (LvId) REFERENCES HR_LvRegister(Lv_Id)
    );

    CREATE INDEX IX_HrLvWfInstance_LvId       ON HrLvWfInstance(LvId);
    CREATE INDEX IX_HrLvWfInstance_WfInstId   ON HrLvWfInstance(WfInstanceId);

    PRINT 'Table HrLvWfInstance created.';
END
ELSE
    PRINT 'Table HrLvWfInstance already exists – skipped.';
GO

-- =============================================================================
-- 3. WORKFLOW USER MAPPING
--    Maps qHRM EmpSysId (int) → Dynamic Workflow UserId (GUID).
--    Required because the two systems use different identity keys.
-- =============================================================================
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'HrWfUserMapping')
BEGIN
    CREATE TABLE HrWfUserMapping (
        EmpSysId        INT                 NOT NULL,   -- FK → HrEmpGenInfo.EmpSysId
        WfUserId        UNIQUEIDENTIFIER    NOT NULL,   -- User.Id from WF Engine
        WfUsername      NVARCHAR(100)       NOT NULL,   -- Username in WF Engine
        CreatedDate     DATETIME            NOT NULL DEFAULT GETDATE(),
        ModifiedDate    DATETIME            NULL,

        CONSTRAINT PK_HrWfUserMapping PRIMARY KEY (EmpSysId),
        CONSTRAINT UQ_HrWfUserMapping_WfUserId UNIQUE (WfUserId)
    );

    PRINT 'Table HrWfUserMapping created.';
END
ELSE
    PRINT 'Table HrWfUserMapping already exists – skipped.';
GO

-- =============================================================================
-- 4. APPROVAL DELEGATION
--    HR-side record for approval delegation (mirror of WF engine delegation).
--    Stores delegation intent. Actual delegation is applied in the WF engine.
-- =============================================================================
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'HrLvWfDelegation')
BEGIN
    CREATE TABLE HrLvWfDelegation (
        DelegId         INT IDENTITY(1,1)   NOT NULL PRIMARY KEY,
        DlgtrEmpId      INT                 NOT NULL,   -- Delegating employee → HR_EmpGenInfo
        DlgteEmpId      INT                 NOT NULL,   -- Delegate (receives approvals) → HR_EmpGenInfo
        LvTypId         INT                 NULL,        -- NULL = all leave types
        FromDate        DATE                NOT NULL,
        ToDate          DATE                NOT NULL,
        Reason          NVARCHAR(500)       NULL,
        -- WF Engine delegation ID (set after creating delegation in WF engine)
        WfDelegationId  UNIQUEIDENTIFIER    NULL,
        -- Audit
        RecStat         SMALLINT            NOT NULL DEFAULT 1, -- 1=Active, 0=Inactive
        CrtId           INT                 NOT NULL DEFAULT 0,
        CrtDateTime     DATETIME            NOT NULL DEFAULT GETDATE(),
        MdfId           INT                 NULL,
        MdfDateTime     DATETIME            NULL,

        CONSTRAINT FK_HrLvWfDelegation_Delegator  FOREIGN KEY (DlgtrEmpId) REFERENCES HR_EmpGenInfo(Emp_Sys_Id),
        CONSTRAINT FK_HrLvWfDelegation_Delegate   FOREIGN KEY (DlgteEmpId) REFERENCES HR_EmpGenInfo(Emp_Sys_Id),
        CONSTRAINT FK_HrLvWfDelegation_LvType     FOREIGN KEY (LvTypId)    REFERENCES HR_LvType(LvTyp_Id),
        CONSTRAINT CK_HrLvWfDelegation_Dates      CHECK (ToDate >= FromDate)
    );

    CREATE INDEX IX_HrLvWfDelegation_Delegator  ON HrLvWfDelegation(DlgtrEmpId);
    CREATE INDEX IX_HrLvWfDelegation_Delegate   ON HrLvWfDelegation(DlgteEmpId);

    PRINT 'Table HrLvWfDelegation created.';
END
ELSE
    PRINT 'Table HrLvWfDelegation already exists – skipped.';
GO

-- =============================================================================
-- 5. MODIFY HrLvRegister – Add Workflow Status Columns
--    WfStat: current workflow status (displayed in UI)
--    WfInstId: FK to HrLvWfInstance for quick lookup
-- =============================================================================
-- Check using HR_LvRegister (V26 naming)
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'HR_LvRegister' AND COLUMN_NAME = 'WfStat')
BEGIN
    ALTER TABLE HR_LvRegister
        ADD WfStat   NVARCHAR(40) NULL,   -- Draft|Submitted|PendingHR|PendingMgr|Approved|Rejected
            WfInstId INT          NULL;   -- FK → HrLvWfInstance.WfInstId

    PRINT 'Columns WfStat, WfInstId added to HR_LvRegister.';
END
ELSE
    PRINT 'Columns already exist on HR_LvRegister – skipped.';
GO

-- =============================================================================
-- 6. LEAVE WORKFLOW NOTIFICATION LOG
--    Records every email/notification sent for leave workflow events.
--    Supports audit and retry without using the separate Notification DB.
-- =============================================================================
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'HrLvWfNotifyLog')
BEGIN
    CREATE TABLE HrLvWfNotifyLog (
        LogId           INT IDENTITY(1,1)   NOT NULL PRIMARY KEY,
        LvId            INT                 NOT NULL,
        WfInstId        INT                 NULL,
        EventType       NVARCHAR(50)        NOT NULL, -- Submitted|Approved|Rejected|FinalApproved|Delegated
        ToEmpSysId      INT                 NOT NULL,
        ToEmail         NVARCHAR(100)       NOT NULL,
        Subject         NVARCHAR(200)       NOT NULL,
        Body            NVARCHAR(MAX)       NULL,
        IsSent          BIT                 NOT NULL DEFAULT 0,
        SentDateTime    DATETIME            NULL,
        ErrorMsg        NVARCHAR(500)       NULL,
        RetryCount      INT                 NOT NULL DEFAULT 0,
        CrtDateTime     DATETIME            NOT NULL DEFAULT GETDATE(),

        CONSTRAINT FK_HrLvWfNotifyLog_LvReg FOREIGN KEY (LvId) REFERENCES HR_LvRegister(Lv_Id)
    );

    CREATE INDEX IX_HrLvWfNotifyLog_LvId    ON HrLvWfNotifyLog(LvId);
    CREATE INDEX IX_HrLvWfNotifyLog_IsSent  ON HrLvWfNotifyLog(IsSent);

    PRINT 'Table HrLvWfNotifyLog created.';
END
ELSE
    PRINT 'Table HrLvWfNotifyLog already exists – skipped.';
GO

-- =============================================================================
-- 7. INITIAL LEAVE WORKFLOW CONFIGURATION SEED DATA
--    Configures the 6 leave types with their workflow codes.
--    LvTypId values must match existing HrLvType records.
--    *** UPDATE LvTypId values to match your actual HrLvType IDs ***
-- =============================================================================

-- NOTE: The commented INSERT statements below must be run AFTER verifying
-- the correct LvTypId values from your HrLvType table.
-- Run: SELECT LvTypId, LvTypCode, NameEng FROM HrLvType ORDER BY LvTypId
-- Then update the LvTypId values below accordingly.

/*
-- Example seed (update LvTypId to match your actual data):

MERGE HrLvWfConfig AS target
USING (VALUES
    -- LvTypId, WfCode,         IsEnabled, PolicyRules (JSON)
    (1, 'LV_SICK',      1, '{"MedCertRequired":true,"FullPayDays":14,"HalfPayDays":28,"NoPay":false}'),
    (2, 'LV_CASUAL',    1, '{"AnnualEntitlement":3,"PayrollCutoffDay":20,"NoPay":false}'),
    (3, 'LV_EMERGENCY', 1, '{"NoPay":true}'),
    (4, 'LV_HAJJ',      1, '{"MaxDays":14,"NoPay":true,"OnceInService":true}'),
    (5, 'LV_MATERNITY', 1, '{"EligibleDays":50,"MinServiceYears":1,"NoPay":false,"GenderRequired":"F"}'),
    (6, 'LV_NURSING',   1, '{"HoursPerDay":1,"EligibilityDaysFromMaternity":365,"NoPay":false}')
) AS source (LvTypId, WfCode, IsEnabled, PolicyRules)
ON target.LvTypId = source.LvTypId AND target.WfCode = source.WfCode
WHEN NOT MATCHED THEN
    INSERT (LvTypId, WfCode, IsEnabled, PolicyRules, CrtId, CrtDateTime)
    VALUES (source.LvTypId, source.WfCode, source.IsEnabled, source.PolicyRules, 1, GETDATE());
*/

PRINT 'Seed data section – please update LvTypId values then uncomment and run the MERGE statement above.';
GO

-- =============================================================================
-- 8. UPDATE SysConfig – Add SMTP Credentials
--    The existing SysConfig table stores SMTP settings.
--    Update the record with your actual SMTP server details.
-- =============================================================================

-- Check if SysConfig record exists and display current values:
SELECT Id, Server, Port, FromEmail, ServiceAcct FROM SysConfig;

-- Run this UPDATE with your actual SMTP values:
/*
UPDATE SysConfig SET
    Server      = 'smtp.yourcompany.com',   -- ← Replace with your SMTP server
    Port        = 587,                       -- ← 587 (TLS) or 465 (SSL) or 25
    FromEmail   = 'hr@yourcompany.com',      -- ← HR system sender email
    ServiceAcct = 'hr@yourcompany.com',      -- ← SMTP auth username
    ServiceAcctPwd = 'your_smtp_password'   -- ← SMTP auth password
WHERE Id = 1;
*/

PRINT 'SMTP configuration – please update SysConfig with your actual SMTP server details.';
GO

-- =============================================================================
-- VERIFICATION QUERIES
-- =============================================================================
PRINT '=== Migration Verification ===';

SELECT 'HrLvWfConfig'      AS TableName, COUNT(*) AS RowCount FROM HrLvWfConfig      UNION ALL
SELECT 'HrLvWfInstance'    AS TableName, COUNT(*) AS RowCount FROM HrLvWfInstance     UNION ALL
SELECT 'HrWfUserMapping'   AS TableName, COUNT(*) AS RowCount FROM HrWfUserMapping    UNION ALL
SELECT 'HrLvWfDelegation'  AS TableName, COUNT(*) AS RowCount FROM HrLvWfDelegation   UNION ALL
SELECT 'HrLvWfNotifyLog'   AS TableName, COUNT(*) AS RowCount FROM HrLvWfNotifyLog;

SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'HR_LvRegister'
  AND COLUMN_NAME IN ('WfStat','WfInstId');

PRINT '=== Migration Complete ===';
GO

-- =============================================================================
-- 9. SYSTEM PRIVILEGE SEEDS
--    Adds the two new Leave Workflow privileges to SysSystemPrivilege.
--    After running, go to: SysAdmin → User Group Privileges and assign
--    LV_WF_CFG and LV_WF_DLG to the appropriate user groups (HR Admin, etc.)
--
--    ParentId: Set this to the existing PrvId of the parent "Leave" menu item
--    in your SysSystemPrivilege table. Run the query below to find it:
--    SELECT PrvId, PrvCode, NameEng FROM SysSystemPrivilege WHERE Area = 'Leave'
-- =============================================================================

-- Find the Leave parent ID:
SELECT PrvId, PrvCode, NameEng FROM SysSystemPrivilege WHERE Area = 'Leave' ORDER BY PrvId;
GO

-- Seed new privileges (run AFTER checking/setting the correct ParentId):
MERGE SysSystemPrivilege AS target
USING (VALUES
    -- PrvCode,       NameEng,                     NameArb,                     Area,    Controller,         Action,  ParentId, Icon
    ('LV_WF_CFG', 'Leave Workflow Configuration', 'تكوين سير عمل الإجازة',    'Leave', 'HrLvWfConfig',    'Index',  NULL, 'fa-cogs'),
    ('LV_WF_DLG', 'Leave Approval Delegation',   'تفويض الموافقة على الإجازة','Leave', 'HrLvWfDelegation','Index',  NULL, 'fa-user-clock')
) AS source (PrvCode, NameEng, NameArb, Area, Controller, Action, ParentId, Icon)
ON target.PrvCode = source.PrvCode
WHEN NOT MATCHED THEN
    INSERT (PrvCode, NameEng, NameArb, Area, Controller, Action, ParentId, RecStat, Icon, CrtDateTime)
    VALUES (source.PrvCode, source.NameEng, source.NameArb, source.Area, source.Controller, source.Action,
            -- *** Replace NULL below with the actual PrvId of the Leave parent record ***
            (SELECT TOP 1 PrvId FROM SysSystemPrivilege WHERE Area = 'Leave' AND ParentId IS NULL),
            1, source.Icon, GETDATE());

PRINT 'System privileges for LV_WF_CFG and LV_WF_DLG seeded.';
PRINT 'Next step: Go to SysAdmin → User Group Privileges to assign these to HR Admin group.';
GO

