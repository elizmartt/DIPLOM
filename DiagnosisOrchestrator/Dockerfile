# ── Stage 1: Build ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY DiagnosisOrchestrator/DiagnosisOrchestrator.csproj   DiagnosisOrchestrator/
COPY MedicalDiagnostic.Data/MedicalDiagnostic.Data.csproj MedicalDiagnostic.Data/
RUN dotnet restore DiagnosisOrchestrator/DiagnosisOrchestrator.csproj
COPY DiagnosisOrchestrator/  DiagnosisOrchestrator/
COPY MedicalDiagnostic.Data/ MedicalDiagnostic.Data/
WORKDIR /src/DiagnosisOrchestrator
RUN dotnet publish DiagnosisOrchestrator.csproj -c Release -o /app/publish --no-restore

# ── Stage 2: Runtime ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Explicitly copy appsettings files (publish doesn't always include them)
COPY --from=build /src/DiagnosisOrchestrator/appsettings*.json ./
COPY --from=build /src/DiagnosisOrchestrator/Appsettings*.json ./

EXPOSE 5217
ENV ASPNETCORE_ENVIRONMENT=Docker
ENV ASPNETCORE_URLS=http://0.0.0.0:5217
ENTRYPOINT ["dotnet", "DiagnosisOrchestrator.dll"]