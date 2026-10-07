$ErrorActionPreference = 'Stop'
$secretPath = Join-Path ([Environment]::GetFolderPath('ApplicationData')) 'Microsoft/UserSecrets/horseclub-backend/secrets.json'
if (!(Test-Path -LiteralPath $secretPath)) { throw 'Run Set-AzureSqlConnection.ps1 first.' }
$secrets = Get-Content -Raw -LiteralPath $secretPath | ConvertFrom-Json
$connectionText = $secrets.'ConnectionStrings:SqlServer'
if ([string]::IsNullOrWhiteSpace($connectionText)) { throw 'Azure SQL connection is missing from User Secrets.' }
$connection = [System.Data.SqlClient.SqlConnectionStringBuilder]::new($connectionText)
if ($connection.DataSource -ne 'tcp:hrcms.database.windows.net,1433' -or $connection.InitialCatalog -ne 'HRCMS' -or $connection.IntegratedSecurity -or !$connection.Encrypt -or $connection.TrustServerCertificate -or [string]::IsNullOrEmpty($connection.Password)) {
    throw 'Expected the authenticated, encrypted Azure SQL connection for hrcms.database.windows.net / HRCMS. Run Set-AzureSqlConnection.ps1 first.'
}
$schemaPath = Join-Path $PSScriptRoot '../docs/azure-sql-schema.sql'
$migrationFolder = Join-Path $PSScriptRoot '../HorseClub.DAL/Data/Migrations/SqlServer'
$migrationIds = @(Get-ChildItem -LiteralPath $migrationFolder -File | Where-Object { $_.Name -match '^\d{14}_[A-Za-z0-9_]+\.cs$' } | Sort-Object BaseName | ForEach-Object { $_.BaseName })
if ($migrationIds.Count -eq 0) { throw 'No SQL Server migrations found.' }
$expectedMigrationCount = $migrationIds.Count
$allowedMigrations = ($migrationIds | ForEach-Object { "'$_'" }) -join ', '
$historyCheck = "IF EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId NOT IN ($allowedMigrations)) THROW 51000, 'Unexpected migration history: review before migration.', 1;"
$quotedHistoryCheck = $historyCheck.Replace("'", "''")
$previousPassword = $env:SQLCMDPASSWORD
try {
    $env:SQLCMDPASSWORD = $connection.Password
    $preflight = @"
SET NOCOUNT ON;
IF DB_NAME() <> 'HRCMS' THROW 51000, 'Unexpected target database.', 1;
IF OBJECT_ID(N'dbo.__EFMigrationsHistory') IS NULL AND EXISTS (SELECT 1 FROM sys.tables WHERE is_ms_shipped = 0)
    THROW 51000, 'Existing unmanaged tables: review schema before migration.', 1;
IF OBJECT_ID(N'dbo.__EFMigrationsHistory') IS NOT NULL
    EXEC(N'$quotedHistoryCheck');
SELECT DB_NAME() AS TargetDatabase, COUNT(*) AS ExistingTables FROM sys.tables WHERE is_ms_shipped = 0;
"@
    & sqlcmd -S $connection.DataSource -d $connection.InitialCatalog -U $connection.UserID -N -b -l 30 -Q $preflight
    if ($LASTEXITCODE -ne 0) { throw 'Azure SQL preflight failed; no migration applied.' }
    & sqlcmd -S $connection.DataSource -d $connection.InitialCatalog -U $connection.UserID -N -b -l 30 -i $schemaPath
    if ($LASTEXITCODE -ne 0) { throw 'Migration failed. Review the SQL error before retrying.' }
    $verify = @"
SET NOCOUNT ON;
IF (SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped = 0 AND name <> '__EFMigrationsHistory') <> 31 THROW 51000, 'Unexpected business table count.', 1;
IF (SELECT COUNT(*) FROM dbo.__EFMigrationsHistory) <> $expectedMigrationCount THROW 51000, 'Unexpected migration count.', 1;
SELECT COUNT(*) AS BusinessTables FROM sys.tables WHERE is_ms_shipped = 0 AND name <> '__EFMigrationsHistory';
SELECT MigrationId FROM dbo.__EFMigrationsHistory ORDER BY MigrationId;
"@
    & sqlcmd -S $connection.DataSource -d $connection.InitialCatalog -U $connection.UserID -N -b -l 30 -Q $verify
    if ($LASTEXITCODE -ne 0) { throw 'Schema verification failed.' }
    Write-Host "Azure SQL schema verified: 31 business tables and $expectedMigrationCount migrations."
}
finally {
    $env:SQLCMDPASSWORD = $previousPassword
    $connection = $null
    $connectionText = $null
    $secrets = $null
}
