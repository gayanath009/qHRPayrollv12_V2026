-- =========================================================================
-- Script: Add_Approval_Privileges.sql
-- Description: Registers 'Salary Process Approvals' and 'Encashment Approvals'
--              in SYS_SystemPrivilege under Payroll (Parent_Id = 5).
--              These privileges allow administrators to independently assign
--              access to the approval screens from the User Privileges screen.
-- =========================================================================

-- 1. Insert 'Salary Process Approvals' (SLRY_APP)
IF NOT EXISTS (SELECT 1 FROM SYS_SystemPrivilege WHERE Prv_Code = 'SLRY_APP')
BEGIN
    DECLARE @NewPrvId1 INT = (SELECT ISNULL(MAX(Prv_Id), 0) + 1 FROM SYS_SystemPrivilege);
    INSERT INTO SYS_SystemPrivilege (
        Prv_Id, Prv_Code, Name_Eng, Name_Arb, Controller, Action, 
        Parent_Id, Rec_Stat, Target, Icon, Crt_DateTime, Area
    )
    VALUES (
        @NewPrvId1, 'SLRY_APP', 'Salary Process Approvals', N'موافقات مسير الرواتب', 
        'HrPrSlryPrcApproval', 'Index', 5, 1, '', 'fas fa-clipboard-check', GETDATE(), 'Payroll'
    );
    PRINT 'Inserted SLRY_APP with Prv_Id = ' + CAST(@NewPrvId1 AS VARCHAR(10));
END
ELSE
BEGIN
    PRINT 'SLRY_APP already exists.';
END

-- 2. Insert 'Encashment Approvals' (ENCSH_APP)
IF NOT EXISTS (SELECT 1 FROM SYS_SystemPrivilege WHERE Prv_Code = 'ENCSH_APP')
BEGIN
    DECLARE @NewPrvId2 INT = (SELECT ISNULL(MAX(Prv_Id), 0) + 1 FROM SYS_SystemPrivilege);
    INSERT INTO SYS_SystemPrivilege (
        Prv_Id, Prv_Code, Name_Eng, Name_Arb, Controller, Action, 
        Parent_Id, Rec_Stat, Target, Icon, Crt_DateTime, Area
    )
    VALUES (
        @NewPrvId2, 'ENCSH_APP', 'Encashment Approvals', N'موافقات صرف الإجازات والتذاكر', 
        'HrPrStaffLvTktEncashApproval', 'Index', 5, 1, '', 'fas fa-plane-departure', GETDATE(), 'Payroll'
    );
    PRINT 'Inserted ENCSH_APP with Prv_Id = ' + CAST(@NewPrvId2 AS VARCHAR(10));
END
ELSE
BEGIN
    PRINT 'ENCSH_APP already exists.';
END

-- 3. Automatically grant both privileges to Admin User Group (Grp_Id = 1)
DECLARE @PrvId_SlryApp INT = (SELECT Prv_Id FROM SYS_SystemPrivilege WHERE Prv_Code = 'SLRY_APP');
DECLARE @PrvId_EncshApp INT = (SELECT Prv_Id FROM SYS_SystemPrivilege WHERE Prv_Code = 'ENCSH_APP');

IF @PrvId_SlryApp IS NOT NULL AND NOT EXISTS (SELECT 1 FROM ADM_UserGroupPrivilege WHERE Grp_Id = 1 AND Prv_Id = @PrvId_SlryApp)
BEGIN
    INSERT INTO ADM_UserGroupPrivilege (Grp_Id, Prv_Id, Rec_View, Rec_Add, Rec_Edit, Rec_Delete, Rec_Stat, Crt_DateTime)
    VALUES (1, @PrvId_SlryApp, 1, 1, 1, 1, 1, GETDATE());
    PRINT 'Granted SLRY_APP to Admin Group (Grp_Id = 1)';
END

IF @PrvId_EncshApp IS NOT NULL AND NOT EXISTS (SELECT 1 FROM ADM_UserGroupPrivilege WHERE Grp_Id = 1 AND Prv_Id = @PrvId_EncshApp)
BEGIN
    INSERT INTO ADM_UserGroupPrivilege (Grp_Id, Prv_Id, Rec_View, Rec_Add, Rec_Edit, Rec_Delete, Rec_Stat, Crt_DateTime)
    VALUES (1, @PrvId_EncshApp, 1, 1, 1, 1, 1, GETDATE());
    PRINT 'Granted ENCSH_APP to Admin Group (Grp_Id = 1)';
END
GO
