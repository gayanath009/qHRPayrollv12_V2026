-- ==============================================================================
-- Script: Reset October 2026 Salary Process & Encashment Workflows to Verified
-- Description:
--   Reverts both the October 2026 Salary Process and the Staff Leave Salary &
--   Air Ticket Encashment batches back to the 'Verified' stage (Prc_Stat = 2).
--   Clears all workflow approvals and returns them to the Initiator so the entire
--   approval cycle can be tested from scratch.
-- ==============================================================================

USE qHRMasterV27; -- Or qHRMasterLive
GO

SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
SET QUOTED_IDENTIFIER ON;
GO

PRINT '================================================================';
PRINT '1. RESETTING OCTOBER 2026 SALARY PROCESS';
PRINT '================================================================';

DECLARE @SalaryPrcId INT;
SELECT @SalaryPrcId = Prc_Id 
FROM HR_PrSlryPrcHdr 
WHERE Eft_Year = 2026 AND Eft_Month = 10;

IF @SalaryPrcId IS NOT NULL
BEGIN
    PRINT 'Resetting Salary Process Header (Prc_Id: ' + CAST(@SalaryPrcId AS VARCHAR(10)) + ')...';

    UPDATE HR_PrSlryPrcHdr
    SET 
        Prc_Stat = 2,                   -- 2 = Verified stage
        Wf_Stat = NULL,                 -- Reverted to Draft/Verified
        Wf_Submitted_By = NULL,
        Wf_Submitted_DateTime = NULL,
        Wf_Approved_By = NULL,
        Wf_Approved_DateTime = NULL,
        Wf_Comments = NULL,
        Cmpd_By = NULL,
        Cmpd_DateTime = NULL
    WHERE Prc_Id = @SalaryPrcId;

    -- Clear workflow history
    DELETE FROM HR_Pr_Wf_History 
    WHERE Ref_Id = @SalaryPrcId AND Wf_Type = 'Salary Process';

    PRINT 'Cleared Salary Process workflow history for Prc_Id: ' + CAST(@SalaryPrcId AS VARCHAR(10));
END
ELSE
BEGIN
    PRINT 'No salary process header found for October 2026.';
END
GO

PRINT '================================================================';
PRINT '2. RESETTING OCTOBER 2026 LEAVE SALARY & AIR TICKET ENCASHMENT';
PRINT '================================================================';

DECLARE @EncashHdrId INT;
SELECT @EncashHdrId = EncashHdr_Id
FROM HR_PrStaffLvTktEncashHdr
WHERE Prc_Year = 2026 AND Prc_Month = 10;

IF @EncashHdrId IS NOT NULL
BEGIN
    PRINT 'Resetting Encashment Header (EncashHdr_Id: ' + CAST(@EncashHdrId AS VARCHAR(10)) + ')...';

    UPDATE HR_PrStaffLvTktEncashHdr
    SET 
        Prc_Stat = 2,                   -- 2 = Verified stage
        Wf_Stat = 'Verified',           -- Verified / ready for initiator submission
        Total_Internal_Leave = 17736.67,
        Total_Internal_Ticket = 17950.00,
        Total_Internal_Settlement = 35686.67,
        Total_Outsource_Leave = 8666.67,
        Total_Outsource_Ticket = 8150.00,
        Total_Outsource_Settlement = 16816.67,
        Grand_Total_Settlement = 52503.34,
        Vrfd_By = 1,
        Vrfd_DateTime = GETDATE(),
        Wf_Submitted_By = NULL,
        Wf_Submitted_DateTime = NULL,
        Wf_Approved_By = NULL,
        Wf_Approved_DateTime = NULL,
        Wf_Comments = NULL
    WHERE EncashHdr_Id = @EncashHdrId;

    UPDATE HR_PrStaffLvTktEncash
    SET Prc_Stat = 2                    -- 2 = Confirmed / Verified
    WHERE Prc_Year = 2026 AND Prc_Month = 10;

    -- Clear workflow history
    DELETE FROM HR_Pr_Wf_History 
    WHERE Prc_Year = 2026 AND Prc_Month = 10 
      AND Wf_Type = 'Leave Salary & Air Ticket Encashment';

    PRINT 'Cleared Encashment workflow history for October 2026.';
END
ELSE
BEGIN
    PRINT 'No encashment header found for October 2026.';
END
GO

PRINT '================================================================';
PRINT '3. CLEARING PENDING NOTIFICATIONS';
PRINT '================================================================';

IF DB_ID('notification') IS NOT NULL
BEGIN
    DELETE FROM notification.dbo.NTF_Notification 
    WHERE Ntf_Typ_Ref_Id = 2 
      AND Req_Id IN (
          SELECT Prc_Id FROM HR_PrSlryPrcHdr WHERE Eft_Year = 2026 AND Eft_Month = 10
          UNION
          SELECT EncashHdr_Id FROM HR_PrStaffLvTktEncashHdr WHERE Prc_Year = 2026 AND Prc_Month = 10
      );

    PRINT 'Cleared pending approval notifications from NTF_Notification.';
END
GO

PRINT '================================================================';
PRINT 'RESET COMPLETE: Both processes are in Verified stage for Initiator';
PRINT '================================================================';
GO
