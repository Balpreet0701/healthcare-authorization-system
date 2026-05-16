# AI-Powered Healthcare Authorization System

Enterprise-style prior authorization platform built for a .NET developer portfolio. The project includes a React/Bootstrap frontend and an ASP.NET Core 8 Web API backed by EF Core and SQL Server.

## Tech Stack

- Frontend: React, JavaScript, HTML, CSS, Bootstrap, SignalR client
- Backend: ASP.NET Core 8 Web API, EF Core, SQL Server, ASP.NET Identity, JWT
- AI and processing: Ollama with Llama3, Tesseract OCR, Hangfire
- Enterprise components: RabbitMQ, Serilog, SignalR, Docker, audit logging

## Features

- JWT authentication with role-based access for Admin, Intake, and Reviewer
- Patient management
- Authorization request workflow
- Medical document upload
- OCR text extraction with Tesseract
- AI medical summary generation using Ollama/Llama3
- AI approval recommendation engine
- Reviewer dashboard and decision workflow
- Real-time notifications with SignalR
- Audit logging
- Analytics dashboard
- File and URL attachments

## Local SQL Server

Your machine has `SQL Server (SQLEXPRESS)` running. The API is configured for Windows Authentication:

```text
Server=.\SQLEXPRESS;Database=HealthcareAuthDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True
```

The API creates the database and seed data on startup.

If SQL Server returns `CREATE DATABASE permission denied in database 'master'`, run [scripts/create-database-admin.sql](scripts/create-database-admin.sql) in SSMS using a login with `sysadmin` or `dbcreator`, then start the API again. Your current Windows login can connect to `.\SQLEXPRESS`, but it needs database ownership or create-database permission for first-time setup.

Seed users:

```text
admin@healthauth.local / Admin@12345
reviewer@healthauth.local / Reviewer@12345
intake@healthauth.local / Intake@12345
```

## Run Locally

For a full clean-machine setup, read [docs/LOCAL_SETUP_AFTER_CLONE.md](docs/LOCAL_SETUP_AFTER_CLONE.md).

Backend:

```powershell
dotnet restore .\src\HealthcareAuth.Api\HealthcareAuth.Api.csproj
dotnet run --project .\src\HealthcareAuth.Api\HealthcareAuth.Api.csproj
```

API endpoints:

```text
Swagger: http://localhost:5088/swagger
Hangfire: http://localhost:5088/hangfire
SignalR hub: http://localhost:5088/hubs/notifications
```

Frontend:

```powershell
cd .\ClientApp
npm install
npm run dev
```

Client:

```text
http://localhost:5173
```

If the ASP.NET Core backend is not running, the React app falls back to built-in demo data so you can preview the UI screens locally.

## Ollama and Tesseract

Install and run Ollama, then pull Llama3:

```powershell
ollama pull llama3
ollama serve
```

Install Tesseract OCR and make sure `tesseract` is available on the PATH. Image uploads use Tesseract for OCR. Text uploads are read directly. PDF uploads are stored and flagged for PDF-to-image conversion.

## Verify Database

After starting the API, you can verify the database in SSMS or with:

```powershell
sqlcmd -S .\SQLEXPRESS -E -i .\scripts\check-database.sql
```

## Docker

```powershell
docker compose up --build
```

Docker starts SQL Server, RabbitMQ, Ollama, the API, and the React client. Pull the Llama3 model inside the Ollama container before relying on AI responses:

```powershell
docker exec -it create-a-complete-enterprise-level-ai-ollama-1 ollama pull llama3
```

RabbitMQ management UI:

```text
http://localhost:15672
guest / guest
```

## Resume Positioning

Suggested resume bullet:

```text
Built an AI-powered healthcare prior authorization platform using ASP.NET Core 8, EF Core, SQL Server, React, JWT/RBAC, SignalR, Hangfire, RabbitMQ, Serilog, Ollama/Llama3, and Tesseract OCR to automate document extraction, medical summarization, reviewer workflows, audit logging, and analytics.
```
