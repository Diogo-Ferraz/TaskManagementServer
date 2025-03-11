USE master;
GO

IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = 'TaskManagementDb')
BEGIN
    CREATE DATABASE TaskManagementDb;
    PRINT 'TaskManagementDb created successfully';
END
ELSE
BEGIN
    PRINT 'TaskManagementDb already exists';
END;
GO

USE TaskManagementDb;
GO
