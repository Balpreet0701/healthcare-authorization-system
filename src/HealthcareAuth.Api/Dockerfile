FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
RUN apt-get update \
    && apt-get install -y --no-install-recommends tesseract-ocr \
    && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/HealthcareAuth.Api/HealthcareAuth.Api.csproj", "src/HealthcareAuth.Api/"]
RUN dotnet restore "src/HealthcareAuth.Api/HealthcareAuth.Api.csproj"
COPY . .
WORKDIR "/src/src/HealthcareAuth.Api"
RUN dotnet publish "HealthcareAuth.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "HealthcareAuth.Api.dll"]
