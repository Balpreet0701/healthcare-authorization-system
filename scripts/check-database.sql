SELECT name, create_date
FROM sys.databases
WHERE name = 'HealthcareAuthDb';

USE HealthcareAuthDb;

SELECT COUNT(*) AS PatientCount FROM dbo.Patients;
SELECT COUNT(*) AS AuthorizationRequestCount FROM dbo.AuthorizationRequests;
SELECT COUNT(*) AS AuditLogCount FROM dbo.AuditLogs;
