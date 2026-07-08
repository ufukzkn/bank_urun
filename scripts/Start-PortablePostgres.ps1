param(
    [string]$PgRoot = ".\.tools\postgres",
    [string]$PgZipPath = "",
    [string]$DataDir = ".\.data\postgres",
    [int]$Port = 5432,
    [string]$PostgresPassword = "postgres",
    [string]$Database = "bank_urun",
    [string]$DatabaseUser = "bank_urun",
    [string]$DatabasePassword = "bank_urun"
)

$ErrorActionPreference = "Stop"

function Resolve-FullPath([string]$PathValue) {
    $executionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($PathValue)
}

function Find-PgBin([string]$Root) {
    $rootFullPath = Resolve-FullPath $Root
    $directBin = Join-Path $rootFullPath "bin"
    if (Test-Path (Join-Path $directBin "postgres.exe")) {
        return $directBin
    }

    $pgsqlBin = Join-Path $rootFullPath "pgsql\bin"
    if (Test-Path (Join-Path $pgsqlBin "postgres.exe")) {
        return $pgsqlBin
    }

    $postgresExe = Get-ChildItem -Path $rootFullPath -Filter "postgres.exe" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($postgresExe) {
        return $postgresExe.Directory.FullName
    }

    throw "postgres.exe bulunamadi. PgRoot: $rootFullPath"
}

$pgRootFullPath = Resolve-FullPath $PgRoot
$dataDirFullPath = Resolve-FullPath $DataDir

if (!(Test-Path $pgRootFullPath) -and $PgZipPath) {
    New-Item -ItemType Directory -Path $pgRootFullPath -Force | Out-Null
    Expand-Archive -Path (Resolve-FullPath $PgZipPath) -DestinationPath $pgRootFullPath -Force
}

if (!(Test-Path $pgRootFullPath)) {
    throw "PostgreSQL portable klasoru yok. -PgRoot veya -PgZipPath verin."
}

$pgBin = Find-PgBin $pgRootFullPath
$initDb = Join-Path $pgBin "initdb.exe"
$pgCtl = Join-Path $pgBin "pg_ctl.exe"
$psql = Join-Path $pgBin "psql.exe"

if (!(Test-Path $dataDirFullPath)) {
    New-Item -ItemType Directory -Path $dataDirFullPath -Force | Out-Null
}

if (!(Test-Path (Join-Path $dataDirFullPath "PG_VERSION"))) {
    $pwFile = Join-Path ([System.IO.Path]::GetTempPath()) ("pg_pw_" + [Guid]::NewGuid().ToString("N") + ".txt")
    Set-Content -Path $pwFile -Value $PostgresPassword -NoNewline
    try {
        & $initDb -D $dataDirFullPath -U postgres -A scram-sha-256 --pwfile $pwFile | Write-Host
    }
    finally {
        Remove-Item -Path $pwFile -Force -ErrorAction SilentlyContinue
    }
}

$statusOutput = & $pgCtl -D $dataDirFullPath status 2>$null
if ($LASTEXITCODE -ne 0) {
    $logPath = Join-Path $dataDirFullPath "postgres.log"
    & $pgCtl -D $dataDirFullPath -l $logPath -o "-p $Port -h 127.0.0.1" -w start | Write-Host
}

$env:PGPASSWORD = $PostgresPassword

$roleExists = (& $psql -h 127.0.0.1 -p $Port -U postgres -d postgres -tAc "select 1 from pg_roles where rolname = '$DatabaseUser';").Trim()
if ($roleExists -ne "1") {
    & $psql -h 127.0.0.1 -p $Port -U postgres -d postgres -c "create user $DatabaseUser with password '$DatabasePassword';" | Write-Host
}

$dbExists = (& $psql -h 127.0.0.1 -p $Port -U postgres -d postgres -tAc "select 1 from pg_database where datname = '$Database';").Trim()
if ($dbExists -ne "1") {
    & $psql -h 127.0.0.1 -p $Port -U postgres -d postgres -c "create database $Database owner $DatabaseUser;" | Write-Host
}

Write-Host "Portable PostgreSQL hazir: 127.0.0.1:$Port / db=$Database / user=$DatabaseUser"
