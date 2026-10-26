param()
$ErrorActionPreference = 'Stop'
$backendProject = Join-Path $PSScriptRoot '../Horse_BackEnd/Horse_BackEnd.csproj'
$localConnection = 'Server=.\SQLEXPRESS;Database=HRCMS;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
$probe = [System.Data.SqlClient.SqlConnection]::new($localConnection)
try {
    $probe.Open()
    $command = $probe.CreateCommand()
    $command.CommandText = 'SELECT DB_NAME()'
    if ($command.ExecuteScalar() -ne 'HRCMS') { throw 'Unexpected local database.' }
}
finally { $probe.Dispose() }

$secretPath = Join-Path $env:APPDATA 'Microsoft/UserSecrets/horseclub-backend/secrets.json'
$backupPath = $null
if (Test-Path -LiteralPath $secretPath) {
    $saved = Get-Content -Raw -LiteralPath $secretPath | ConvertFrom-Json
    if ($saved.'ConnectionStrings:SqlServer' -ne $localConnection) {
        $directory = Join-Path $env:LOCALAPPDATA 'HRCMS/connections'
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
        $backupPath = Join-Path $directory ('sql-before-local-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [guid]::NewGuid().ToString('N') + '.private.json')
        $previous = [ordered]@{}
        foreach ($key in @('ConnectionStrings:SqlServer', 'Database:Provider', 'Database:AutoMigrate')) {
            if ($saved.PSObject.Properties.Name -contains $key) { $previous[$key] = $saved.$key }
        }
        $previous | ConvertTo-Json | Set-Content -LiteralPath $backupPath -Encoding utf8
    }
}
@{
    'ConnectionStrings:SqlServer' = $localConnection
    'Database:Provider' = 'SqlServer'
    'Database:AutoMigrate' = 'false'
} | ConvertTo-Json -Compress | dotnet user-secrets set --project $backendProject
if ($LASTEXITCODE -ne 0) { throw 'Could not save local SQL settings.' }
Write-Host 'Local SQL verified and saved: .\SQLEXPRESS / HRCMS (Windows authentication).'
if ($backupPath) { Write-Host ('Previous SQL settings saved privately: ' + $backupPath) }
