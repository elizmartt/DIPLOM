FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ApiGateway/ApiGateway.csproj                         ApiGateway/
COPY MedicalDiagnostic.Data/MedicalDiagnostic.Data.csproj MedicalDiagnostic.Data/

RUN dotnet restore ApiGateway/ApiGateway.csproj

COPY ApiGateway/           ApiGateway/
COPY MedicalDiagnostic.Data/ MedicalDiagnostic.Data/

WORKDIR /src/ApiGateway
RUN dotnet publish ApiGateway.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 5169

ENV ASPNETCORE_ENVIRONMENT=Docker
ENV ASPNETCORE_URLS=http://0.0.0.0:5169

ENTRYPOINT ["dotnet", "ApiGateway.dll"]
