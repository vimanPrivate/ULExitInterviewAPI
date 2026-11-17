IF OBJECT_ID(N'dbo.tblBusinessPartners', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblBusinessPartners
    (
        ID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tblBusinessPartners PRIMARY KEY,
        MasterStaffId VARCHAR(20) NOT NULL,
        IsMasterActive BIT NOT NULL CONSTRAINT DF_tblBusinessPartners_IsMasterActive DEFAULT (1),
        ActingStaffID VARCHAR(20) NULL,
        CreatedDate DATETIME NOT NULL CONSTRAINT DF_tblBusinessPartners_CreatedDate DEFAULT (GETDATE()),
        CreatedUser VARCHAR(20) NOT NULL,
        UpdatedDate DATETIME NULL,
        UpdatedUser VARCHAR(20) NULL,
        IsActive BIT NULL CONSTRAINT DF_tblBusinessPartners_IsActive DEFAULT (1)
    );
END;

IF COL_LENGTH(N'dbo.tblBusinessPartners', N'IsActive') IS NULL
BEGIN
    ALTER TABLE dbo.tblBusinessPartners
        ADD IsActive BIT NULL CONSTRAINT DF_tblBusinessPartners_IsActive DEFAULT (1) WITH VALUES;
END;
