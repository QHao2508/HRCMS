# Run in the same PowerShell session/user profile that saved the Azure User Secrets.
# This saves a local, Git-ignored copy for workspace tools; it does not modify the database.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Data
$backendProject = Join-Path $PSScriptRoot '../Horse_BackEnd/Horse_BackEnd.csproj'
$localPath = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../Horse_BackEnd/appsettings.AzureAccess.Local.json'))
$secretLines = $null
$connectionText = $null
$connection = $null
$probe = $null
try {
    $secretLines = @(dotnet user-secrets list --project $backendProject)
    if ($LASTEXITCODE -ne 0) { throw 'Cannot read the project User Secrets. No local copy saved.' }
    $line = $secretLines | Where-Object { $_ -match '^ConnectionStrings:SqlServer\s*=\s*' } | Select-Object -First 1
    if (!$line) { throw 'The Azure SQL secret is missing in this user profile. Run Set-AzureSqlConnection.ps1 in this terminal first.' }
    $connectionText = $line -replace '^ConnectionStrings:SqlServer\s*=\s*', ''
    $connection = [System.Data.SqlClient.SqlConnectionStringBuilder]::new($connectionText)
    if ($connection.DataSource -ne 'tcp:hrcms.database.windows.net,1433' -or $connection.InitialCatalog -ne 'HRCMS' -or $connection.IntegratedSecurity -or !$connection.Encrypt -or $connection.TrustServerCertificate -or !$connection.Password) {
        throw 'The secret is not the expected encrypted Azure HRCMS connection. No local copy saved.'
    }
    $probe = [System.Data.SqlClient.SqlConnection]::new($connection.ConnectionString)
    try {
        $probe.Open()
        $command = $probe.CreateCommand()
        $command.CommandText = 'SELECT DB_NAME();'
        if ($command.ExecuteScalar() -ne 'HRCMS') { throw 'Unexpected database. No local copy saved.' }
    }
    catch [System.Data.SqlClient.SqlException] {
        throw "Connection verification failed (SQL error $($_.Exception.Number)). No local copy saved."
    }
    $settings = @{ 'ConnectionStrings:SqlServer' = $connection.ConnectionString }
    [IO.File]::WriteAllText($localPath, ($settings | ConvertTo-Json), [Text.UTF8Encoding]::new($false))
    Write-Host 'Azure HRCMS connection verified. Local access copy saved for workspace tools.'
    Write-Host "Local file: $localPath"
    Write-Host 'This file contains credentials and is ignored by Git. Do not upload or paste its contents into chat.'
    Write-Host 'No tables or data have been changed.'
}
finally {
    if ($null -ne $probe) { $probe.Dispose() }
    $secretLines = $null
    $connectionText = $null
    $connection = $null
    $settings = $null
    $line = $null
}
