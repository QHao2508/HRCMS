param(
    [string]$UserName = 'hrcms'
)

$ErrorActionPreference = 'Stop'
$backendProject = Join-Path $PSScriptRoot '../Horse_BackEnd/Horse_BackEnd.csproj'
$securePassword = Read-Host 'Azure SQL password for hrcms.database.windows.net / HRCMS' -AsSecureString
$passwordPointer = [IntPtr]::Zero
try {
    $passwordPointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
    $connection = [System.Data.SqlClient.SqlConnectionStringBuilder]::new()
    # Windows PowerShell treats property assignment on this dictionary as a key.
    # Use supported connection-string keywords explicitly.
    $connection['Data Source'] = 'tcp:hrcms.database.windows.net,1433'
    $connection['Initial Catalog'] = 'HRCMS'
    $connection['User ID'] = $UserName
    $connection['Password'] = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($passwordPointer)
    $connection['Encrypt'] = $true
    $connection['TrustServerCertificate'] = $false
    $connection['Connect Timeout'] = 30
    if ([string]::IsNullOrWhiteSpace($connection.Password)) { throw 'Password cannot be empty.' }

    # Send JSON through stdin, never through command-line arguments or console output.
    $settings = @{
        'ConnectionStrings:SqlServer' = $connection.ConnectionString
        'Database:Provider' = 'SqlServer'
        'Database:AutoMigrate' = 'false'
    } | ConvertTo-Json -Compress
    $settings | dotnet user-secrets set --project $backendProject
    if ($LASTEXITCODE -ne 0) { throw 'Could not save Azure SQL settings.' }
    Write-Host 'Azure SQL connection saved to User Secrets. No tables have been changed yet.'
}
finally {
    if ($passwordPointer -ne [IntPtr]::Zero) { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($passwordPointer) }
    $securePassword.Dispose()
    $settings = $null
    $connection = $null
}
