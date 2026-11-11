IF OBJECT_ID(N'dbo.tblBpAssignedCostCenters', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblBpAssignedCostCenters
    (
        ID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tblBpAssignedCostCenters PRIMARY KEY,
        tblBusinessPartners_ID INT NOT NULL,
        Division NVARCHAR(200) NOT NULL,
        CostCenter NVARCHAR(200) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_tblBpAssignedCostCenters_IsActive DEFAULT (1),
        CONSTRAINT FK_tblBpAssignedCostCenters_BusinessPartner FOREIGN KEY (tblBusinessPartners_ID)
            REFERENCES dbo.tblBusinessPartners(ID),
        CONSTRAINT UQ_tblBpAssignedCostCenters_Assignment UNIQUE (tblBusinessPartners_ID, Division, CostCenter)
    );
END;

IF COL_LENGTH(N'dbo.tblBpAssignedCostCenters', N'IsActive') IS NULL
BEGIN
    ALTER TABLE dbo.tblBpAssignedCostCenters
        ADD IsActive BIT NOT NULL CONSTRAINT DF_tblBpAssignedCostCenters_IsActive DEFAULT (1) WITH VALUES;
END;
