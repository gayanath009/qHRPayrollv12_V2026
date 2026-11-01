-- ==============================================================================
-- Migration: Add ORG_Religion table and Rel_Id column in HR_EmpGenInfo
-- ==============================================================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ORG_Religion')
BEGIN
    CREATE TABLE [dbo].[ORG_Religion] (
        [Rel_Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Rel_Name_Eng] NVARCHAR(50) NOT NULL,
        [Rel_Name_Arb] NVARCHAR(50) NULL,
        [Rec_Stat] SMALLINT NOT NULL DEFAULT 1,
        [Crt_DateTime] DATETIME NOT NULL DEFAULT GETDATE()
    );

    INSERT INTO [dbo].[ORG_Religion] ([Rel_Name_Eng], [Rec_Stat]) VALUES
    (N'Islam', 1),
    (N'Christianity', 1),
    (N'Hinduism', 1),
    (N'Buddhism', 1),
    (N'Sikhism', 1);
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HR_EmpGenInfo]') AND name = 'Rel_Id')
BEGIN
    ALTER TABLE [dbo].[HR_EmpGenInfo] ADD [Rel_Id] INT NULL;
    ALTER TABLE [dbo].[HR_EmpGenInfo] ADD CONSTRAINT [FK_HR_EmpGenInfo_ORG_Religion] FOREIGN KEY ([Rel_Id]) REFERENCES [dbo].[ORG_Religion]([Rel_Id]);
END
GO
