-- ==============================================================================
-- Script: Reset October 2026 Salary Process to Verified Stage
-- Description:
--   Reverts the October 2026 salary process back to the 'Verified' stage
--   (Prc_Stat = 2) and resets the approval workflow state back to the Initiator.
-- ==============================================================================

USE qHRMasterV27; -- Or qHRMasterLive
GO

-- 1. Identify and reset the salary process header for October 2026
DECLARE @PrcId INT;
SELECT @PrcId = Prc_Id 
FROM HR_PrSlryPrcHdr 
WHERE Eft_Year = 2026 AND Eft_Month = 10;

IF @PrcId IS NOT NULL
BEGIN
    PRINT 'Resetting Salary Process Header for Prc_Id: ' + CAST(@PrcId AS VARCHAR(10));

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
    WHERE Prc_Id = @PrcId;

    -- 2. Clear workflow approval history so workflow starts fresh from Step 1
    DELETE FROM HR_Pr_Wf_History 
    WHERE Ref_Id = @PrcId AND Wf_Type = 'Salary Process';

    PRINT 'Cleared HR_Pr_Wf_History records for Prc_Id: ' + CAST(@PrcId AS VARCHAR(10));
END
ELSE
BEGIN
    PRINT 'No salary process header found for October 2026.';
END
GO

-- 3. Clear pending in-app notifications if notification database exists
IF DB_ID('notification') IS NOT NULL
BEGIN
    USE notification;
    
    DELETE FROM NTF_Notification 
    WHERE Ntf_Typ_Ref_Id = 2 
      AND Req_Id IN (
          SELECT Prc_Id FROM qHRMasterV27.dbo.HR_PrSlryPrcHdr 
          WHERE Eft_Year = 2026 AND Eft_Month = 10
      );

    PRINT 'Cleared pending approval notifications from NTF_Notification.';
END
GO
