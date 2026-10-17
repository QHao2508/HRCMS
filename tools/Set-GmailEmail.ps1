param(
    [Parameter(Mandatory = $true)][string]$Email,
    [string]$FromName = 'HRCMS'
)
$ErrorActionPreference = 'Stop'
if ($Email -notmatch '^[^\s@]+@[^\s@]+\.[^\s@]+$') { throw 'Enter a valid sender email.' }
$backendProject = Join-Path $PSScriptRoot '../Horse_BackEnd/Horse_BackEnd.csproj'
$securePassword = Read-Host 'Google App Password (not your normal account password)' -AsSecureString
$passwordPointer = [IntPtr]::Zero
try {
    $passwordPointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
    $appPassword = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($passwordPointer).Replace(' ', '')
    if ([string]::IsNullOrWhiteSpace($appPassword)) { throw 'App password cannot be empty.' }
    $settings = @{
        'Email:Provider' = 'Smtp'; 'Email:Smtp:Host' = 'smtp.gmail.com'; 'Email:Smtp:Port' = '587'
        'Email:FromAddress' = $Email; 'Email:FromName' = $FromName
        'Email:Smtp:Username' = $Email; 'Email:Smtp:Password' = $appPassword; 'Email:Smtp:EnableSsl' = 'true'
        'Workers:Enabled' = 'true'
    } | ConvertTo-Json -Compress
    $settings | dotnet user-secrets set --project $backendProject
    if ($LASTEXITCODE -ne 0) { throw 'Could not save email settings.' }
    Write-Host 'Gmail SMTP saved to User Secrets. Restart the API to activate real email delivery.'
}
finally {
    if ($passwordPointer -ne [IntPtr]::Zero) { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($passwordPointer) }
    $securePassword.Dispose(); $appPassword = $null; $settings = $null
}
