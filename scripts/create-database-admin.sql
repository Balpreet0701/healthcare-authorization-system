/*
Run this in SSMS using a login that has sysadmin or dbcreator permission.
It creates the local portfolio database and makes your Windows login the owner
so the ASP.NET Core app can create tables and seed data with Windows auth.
*/

USE master;
GO

IF DB_ID(N'HealthcareAuthDb') IS NULL
BEGIN
    CREATE DATABASE [HealthcareAuthDb];
END
GO

ALTER AUTHORIZATION ON DATABASE::[HealthcareAuthDb] TO [PERSISTENT\balpreet_kaur1];
GO

SELECT
    name,
    SUSER_SNAME(owner_sid) AS OwnerName,
    create_date
FROM sys.databases
WHERE name = N'HealthcareAuthDb';
GO
