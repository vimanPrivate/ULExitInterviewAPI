IF OBJECT_ID(N'dbo.tblBpAssignedCostCenters', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblBpAssignedCostCenters
    (
        ID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tblBpAssignedCostCenters PRIMARY KEY,
        tblBusinessPartners_ID INT NOT NULL,
        Division NVARCHAR(200) NOT NULL,
        CostCenter NVARCHAR(200) NOT NULL,
        CONSTRAINT FK_tblBpAssignedCostCenters_BusinessPartner FOREIGN KEY (tblBusinessPartners_ID)
            REFERENCES dbo.tblBusinessPartners(ID),
        CONSTRAINT UQ_tblBpAssignedCostCenters_Assignment UNIQUE (tblBusinessPartners_ID, Division, CostCenter)
    );
END;
