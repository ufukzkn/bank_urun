param(
    [string]$PgRoot = ".\.tools\postgres",
    [string]$DataDir = ".\.data\postgres"
)

$ErrorActionPreference = "Stop"

function Resolve-FullPath([string]$PathValue) {
    $executionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($PathValue)
}

$dataDirFullPath = Resolve-FullPath $DataDir
$pgRootFullPath = Resolve-FullPath $PgRoot
$pgCtl = Get-ChildItem -Path $pgRootFullPath -Filter "pg_ctl.exe" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1

if (!$pgCtl) {
    throw "pg_ctl.exe bulunamadi. PostgreSQL portable klasorunu kontrol edin."
}

& $pgCtl.FullName -D $dataDirFullPath -w stop
