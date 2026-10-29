param(
    [string]$SchemaPath = (Join-Path $PSScriptRoot '../docs/azure-sql-schema.sql'),
    # Must match the database saved by Set-AzureSqlConnection.ps1; HRCMS_SQL_DATABASE overrides the default hrcms2.
    [string]$DatabaseName = $(if ($env:HRCMS_SQL_DATABASE) { $env:HRCMS_SQL_DATABASE } else { 'hrcms2' }),
    [switch]$ValidateOnly
)
$ErrorActionPreference = 'Stop'
$schemaPath = (Resolve-Path -LiteralPath $SchemaPath).Path
$migrationFolder = Join-Path $PSScriptRoot '../HorseClub.DAL/Data/Migrations/SqlServer'
$migrationIds = @(Get-ChildItem -LiteralPath $migrationFolder -File | Where-Object { $_.Name -match '^\d{14}_[A-Za-z0-9_]+\.cs$' } | Sort-Object BaseName | ForEach-Object { $_.BaseName })
if ($migrationIds.Count -eq 0) { throw 'No SQL Server migrations found.' }
$expectedMigrationCount = $migrationIds.Count
$snapshotPath = Join-Path $migrationFolder 'SqlServerClubDbContextModelSnapshot.cs'
$expectedTableCount = ([regex]::Matches([System.IO.File]::ReadAllText($snapshotPath), '\.ToTable\(')).Count
if ($expectedTableCount -lt 1) { throw 'No application tables found in the EF model snapshot.' }
# Check the deploy artifact before reading credentials or touching the target database.
$schemaText = [System.IO.File]::ReadAllText($schemaPath)
$schemaMigrationIds = @([regex]::Matches($schemaText, "(?im)^\s*VALUES\s*\(N'(?<id>\d{14}_[A-Za-z0-9_]+)'") | ForEach-Object { $_.Groups['id'].Value } | Sort-Object -Unique)
$missingSql = @($migrationIds | Where-Object { $_ -notin $schemaMigrationIds })
$unknownSql = @($schemaMigrationIds | Where-Object { $_ -notin $migrationIds })
if ($missingSql.Count -gt 0 -or $unknownSql.Count -gt 0) {
    throw "Schema SQL does not match DAL migrations. Missing: $($missingSql -join ', '). Unknown: $($unknownSql -join ', '). Regenerate docs/azure-sql-schema.sql with dotnet ef migrations script --idempotent --project HorseClub.DAL --startup-project Horse_BackEnd --context SqlServerClubDbContext --output docs/azure-sql-schema.sql. No database connection was opened."
}
if ($ValidateOnly) {
    Write-Host "Schema artifact verified: $expectedTableCount application tables and $expectedMigrationCount migrations. No database connection was opened."
    return
}
$secretPath = Join-Path ([Environment]::GetFolderPath('ApplicationData')) 'Microsoft/UserSecrets/horseclub-backend/secrets.json'
$secrets = if (Test-Path -LiteralPath $secretPath) { Get-Content -Raw -LiteralPath $secretPath | ConvertFrom-Json } else { $null }
$connectionText = $secrets.'ConnectionStrings:SqlServer'
if ([string]::IsNullOrWhiteSpace($connectionText)) {
    $accessPath = Join-Path $PSScriptRoot '../Horse_BackEnd/appsettings.AzureAccess.Local.json'
    if (Test-Path -LiteralPath $accessPath) {
        $secrets = Get-Content -Raw -LiteralPath $accessPath | ConvertFrom-Json
        $connectionText = $secrets.'ConnectionStrings:SqlServer'
    }
}
if ([string]::IsNullOrWhiteSpace($connectionText)) { throw 'Azure SQL connection is missing from User Secrets.' }
$connection = [System.Data.SqlClient.SqlConnectionStringBuilder]::new($connectionText)
if ($connection.DataSource -ne 'tcp:hrcms.database.windows.net,1433' -or $connection.InitialCatalog -ne $DatabaseName -or $connection.IntegratedSecurity -or !$connection.Encrypt -or $connection.TrustServerCertificate -or [string]::IsNullOrEmpty($connection.Password)) {
    throw "Expected the authenticated, encrypted Azure SQL connection for hrcms.database.windows.net / $DatabaseName. Run Set-AzureSqlConnection.ps1 first."
}
$allowedMigrations = ($migrationIds | ForEach-Object { "'$_'" }) -join ', '
$historyCheck = "IF EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId NOT IN ($allowedMigrations)) THROW 51000, 'Unexpected migration history: review before migration.', 1;"
$quotedHistoryCheck = $historyCheck.Replace("'", "''")
$previousPassword = $env:SQLCMDPASSWORD
try {
    $env:SQLCMDPASSWORD = $connection.Password
    $preflight = @"
SET NOCOUNT ON;
IF DB_NAME() <> N'$DatabaseName' THROW 51000, 'Unexpected target database.', 1;
IF OBJECT_ID(N'dbo.__EFMigrationsHistory') IS NULL AND EXISTS (SELECT 1 FROM sys.tables WHERE is_ms_shipped = 0)
    THROW 51000, 'Existing unmanaged tables: review schema before migration.', 1;
IF OBJECT_ID(N'dbo.__EFMigrationsHistory') IS NOT NULL
    EXEC(N'$quotedHistoryCheck');
SELECT DB_NAME() AS TargetDatabase, COUNT(*) AS ExistingTables FROM sys.tables WHERE is_ms_shipped = 0;
"@
    & sqlcmd -S $connection.DataSource -d $connection.InitialCatalog -U $connection.UserID -N -b -l 30 -Q $preflight
    if ($LASTEXITCODE -ne 0) { throw 'Azure SQL preflight failed; no migration applied.' }
    # The schema contains Vietnamese seed data; never let the Windows ANSI code page decode UTF-8 input.
    & sqlcmd -S $connection.DataSource -d $connection.InitialCatalog -U $connection.UserID -N -I -b -f 65001 -l 30 -i $schemaPath
    if ($LASTEXITCODE -ne 0) { throw 'Migration failed. Review the SQL error before retrying.' }
    $verify = @"
SET NOCOUNT ON;
IF (SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped = 0 AND name <> '__EFMigrationsHistory') <> $expectedTableCount THROW 51000, 'Unexpected business table count.', 1;
IF (SELECT COUNT(*) FROM dbo.__EFMigrationsHistory) <> $expectedMigrationCount THROW 51000, 'Unexpected migration count.', 1;
SELECT COUNT(*) AS BusinessTables FROM sys.tables WHERE is_ms_shipped = 0 AND name <> '__EFMigrationsHistory';
SELECT MigrationId FROM dbo.__EFMigrationsHistory ORDER BY MigrationId;
"@
    & sqlcmd -S $connection.DataSource -d $connection.InitialCatalog -U $connection.UserID -N -b -l 30 -Q $verify
    if ($LASTEXITCODE -ne 0) { throw 'Schema verification failed.' }
    Write-Host "Azure SQL schema verified: $expectedTableCount application tables and $expectedMigrationCount migrations."
}
finally {
    $env:SQLCMDPASSWORD = $previousPassword
    $connection = $null
    $connectionText = $null
    $secrets = $null
}
