FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY NuGet.config BankUrun.slnx ./
COPY BankUrun.Web/BankUrun.Web.csproj BankUrun.Web/
RUN dotnet restore BankUrun.Web/BankUrun.Web.csproj --configfile NuGet.config

COPY . .
RUN dotnet publish BankUrun.Web/BankUrun.Web.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY --from=build /src/scripts/seed-mock-data.sql ./scripts/seed-mock-data.sql

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "BankUrun.Web.dll"]
