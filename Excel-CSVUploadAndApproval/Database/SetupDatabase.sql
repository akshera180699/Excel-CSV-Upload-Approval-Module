IF DB_ID(N'ExcelUploadApprovalDb') IS NULL
BEGIN
    CREATE DATABASE ExcelUploadApprovalDb;
END
GO

USE ExcelUploadApprovalDb;
GO


IF OBJECT_ID(N'dbo.UploadBatches', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UploadBatches
    (
        BatchId INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_UploadBatches PRIMARY KEY,

        OriginalFileName NVARCHAR(255) NOT NULL,

        TableName SYSNAME NOT NULL,

        UploadedAt DATETIME2 NOT NULL
            CONSTRAINT DF_UploadBatches_UploadedAt
            DEFAULT SYSUTCDATETIME()
    );
END
GO
