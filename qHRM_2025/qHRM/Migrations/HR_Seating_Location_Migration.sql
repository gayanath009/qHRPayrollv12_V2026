-- ==============================================================================
-- Migration: Add Seating_Location column in HR_EmpGenInfo
-- ==============================================================================

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HR_EmpGenInfo]') AND name = 'Seating_Location')
BEGIN
    ALTER TABLE [dbo].[HR_EmpGenInfo] ADD [Seating_Location] NVARCHAR(150) NULL;
END
GO
