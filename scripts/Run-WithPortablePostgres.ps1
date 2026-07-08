param(
    [string]$PgRoot = ".\.tools\postgres",
    [string]$PgZipPath = "",
    [switch]$Seed
)

$ErrorActionPreference = "Stop"

$startArgs = @{
    PgRoot = $PgRoot
}

if ($PgZipPath) {
    $startArgs.PgZipPath = $PgZipPath
}

& "$PSScriptRoot\Start-PortablePostgres.ps1" @startArgs

dotnet tool restore
dotnet tool run dotnet-ef database update --project BankUrun.Web\BankUrun.Web.csproj --startup-project BankUrun.Web\BankUrun.Web.csproj

if ($Seed) {
    $psql = Get-ChildItem -Path $PgRoot -Filter "psql.exe" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
    if (!$psql) {
        throw "psql.exe bulunamadi."
    }

    $env:PGPASSWORD = "bank_urun"
    & $psql.FullName -h 127.0.0.1 -p 5432 -U bank_urun -d bank_urun -f scripts\seed-mock-data.sql
}

dotnet run --project BankUrun.Web\BankUrun.Web.csproj --urls http://localhost:5188
