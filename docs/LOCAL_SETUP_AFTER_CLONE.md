# Local Setup After Cloning

This guide assumes you are running the project on your own laptop/desktop after installing SQL Server and SSMS.

## 1. Install Required Software

Install:

- Git
- Visual Studio 2022 or Visual Studio Code
- .NET 8 SDK
- Node.js LTS
- SQL Server Developer Edition or SQL Server Express
- SQL Server Management Studio
- Ollama
- Tesseract OCR
- Docker Desktop, optional for RabbitMQ/container testing

Recommended SQL Server setup:

- Choose Mixed Mode Authentication.
- Set a strong `sa` password.
- Add your Windows user as a SQL Server administrator during installation.
- Install SSMS after SQL Server.

## 2. Clone The Repository

```powershell
git clone https://github.com/balpreet0701/healthcare-authorization-system.git
cd healthcare-authorization-system
```

## 3. Configure SQL Server Connection

Copy the local config template:

```powershell
copy .\src\HealthcareAuth.Api\appsettings.Local.example.json .\src\HealthcareAuth.Api\appsettings.Local.json
```

Open `src/HealthcareAuth.Api/appsettings.Local.json` and update the SQL password:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=HealthcareAuthDb;User Id=sa;Password=YOUR_SQL_PASSWORD;TrustServerCertificate=True;MultipleActiveResultSets=True"
  }
}
```

If your SQL Server is named `SQLEXPRESS`, use:

```text
Server=localhost\SQLEXPRESS;Database=HealthcareAuthDb;User Id=sa;Password=YOUR_SQL_PASSWORD;TrustServerCertificate=True;MultipleActiveResultSets=True
```

If you prefer Windows Authentication and your Windows user is SQL admin:

```text
Server=localhost\SQLEXPRESS;Database=HealthcareAuthDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True
```

Do not commit `appsettings.Local.json`; it is ignored by git.

## 4. Create And Seed The Database

From the repository root:

```powershell
dotnet restore .\src\HealthcareAuth.Api\HealthcareAuth.Api.csproj
dotnet run --project .\src\HealthcareAuth.Api\HealthcareAuth.Api.csproj -- --seed-only
```

The seed step creates:

- `HealthcareAuthDb`
- Identity tables
- Patient/workflow tables
- Hangfire tables
- Demo users
- Sample patient and authorization request

Demo accounts:

```text
admin@healthauth.local / Admin@12345
reviewer@healthauth.local / Reviewer@12345
intake@healthauth.local / Intake@12345
```

## 5. Run The Backend

```powershell
dotnet run --project .\src\HealthcareAuth.Api\HealthcareAuth.Api.csproj
```

Open:

```text
http://localhost:5088/swagger
http://localhost:5088/hangfire
```

## 6. Run The Frontend

Open a second PowerShell window:

```powershell
cd ClientApp
npm install
npm run dev
```

Open:

```text
http://localhost:5173
```

## 7. Install And Run Ollama

```powershell
ollama pull llama3
ollama serve
```

The app will call:

```text
http://localhost:11434
```

If Ollama is not running, the API still works with deterministic fallback AI responses.

## 8. Install Tesseract OCR

Install Tesseract for Windows and add it to PATH. Verify:

```powershell
tesseract --version
```

Image files use Tesseract OCR. Text files are read directly. PDFs are stored, with a note that PDF-to-image conversion is required for OCR extraction.

## 9. Optional RabbitMQ

For local RabbitMQ without Docker, install RabbitMQ and enable the management plugin.

With Docker:

```powershell
docker compose up rabbitmq
```

RabbitMQ UI:

```text
http://localhost:15672
guest / guest
```

To enable RabbitMQ events, set this in `appsettings.Local.json`:

```json
{
  "RabbitMq": {
    "Enabled": true,
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "guest",
    "Password": "guest",
    "ExchangeName": "healthauth.events"
  }
}
```

## 10. Useful Build Commands

```powershell
dotnet build .\HealthcareAuthorizationSystem.sln
cd ClientApp
npm run build
```
