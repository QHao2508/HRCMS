param([string]$SettingsPath = (Join-Path $PSScriptRoot '../deploy/azure-appsettings.local.json'))

$ErrorActionPreference = 'Stop'
try { $settings = Get-Content -LiteralPath $SettingsPath -Raw | ConvertFrom-Json -AsHashtable }
catch { throw 'Unable to read deployment settings as JSON; contents omitted.' }
if ($settings -isnot [System.Collections.IDictionary]) { throw 'Expected a JSON object of Container Apps environment settings.' }
foreach ($name in @('WEBSITES_PORT', 'WEBSITES_ENABLE_APP_SERVICE_STORAGE')) {
    if ($settings.Contains($name)) { throw "Remove App Service-only setting: $name" }
}
$required = @(
    'ASPNETCORE_ENVIRONMENT', 'ASPNETCORE_HTTP_PORTS',
    'AllowedHosts', 'Cors__Origins__0', 'ConnectionStrings__SqlServer',
    'DataProtection__Path', 'DataProtection__KeyEncryption', 'DataProtection__RequireEncryptedKeys',
    'DataProtection__CertificateBase64', 'DataProtection__CertificatePassword',
    'Storage__Provider', 'Storage__ServiceUri', 'Storage__Container',
    'Workers__Enabled', 'Email__FromAddress', 'Email__Smtp__Host',
    'Email__Smtp__Username', 'Email__Smtp__Password'
)
foreach ($name in $required) {
    if (!$settings.Contains($name) -or [string]::IsNullOrWhiteSpace([string]$settings[$name])) {
        throw "Missing required setting: $name"
    }
}
foreach ($name in $settings.Keys) {
    if ([string]$settings[$name] -match 'REPLACE_WITH_|@Microsoft.KeyVault\(') {
        throw "Replace the placeholder or Key Vault reference in: $name"
    }
}
$expected = @{
    ASPNETCORE_ENVIRONMENT = 'Production'
    ASPNETCORE_HTTP_PORTS = '8080'
    Database__AutoMigrate = 'false'
    DataProtection__Path = '/mnt/hrcms-keys'
    DataProtection__KeyEncryption = 'Certificate'
    DataProtection__RequireEncryptedKeys = 'true'
    Storage__Provider = 'AzureBlob'
    Workers__Enabled = 'true'
}
foreach ($name in $expected.Keys) {
    if ([string]$settings[$name] -cne $expected[$name]) { throw "Unexpected deployment value for: $name" }
}
foreach ($name in @($settings.Keys | Where-Object { $_ -like 'Cors__Origins__*' })) {
    $origin = $null
    if (![Uri]::TryCreate([string]$settings[$name], [UriKind]::Absolute, [ref]$origin) -or
        $origin.Scheme -ne 'https' -or $origin.UserInfo -or $origin.Query -or $origin.Fragment -or
        $origin.AbsolutePath -ne '/' -or [string]$settings[$name] -cne $origin.GetLeftPart([UriPartial]::Authority)) {
        throw "Expected an HTTPS origin without path or trailing slash in: $name"
    }
}
try { $connection = [System.Data.SqlClient.SqlConnectionStringBuilder]::new([string]$settings.ConnectionStrings__SqlServer) }
catch { throw 'Invalid SQL Server connection string; value omitted.' }
if (!$connection.Encrypt -or $connection.TrustServerCertificate -or !$connection.Password -or
    !$connection.UserID -or !$connection.InitialCatalog -or !$connection.DataSource) {
    throw 'SQL connection requires server, database, user, password, Encrypt=True and TrustServerCertificate=False.'
}
try { $certificateBytes = [Convert]::FromBase64String([string]$settings.DataProtection__CertificateBase64) }
catch { throw 'CertificateBase64 must contain Base64-encoded PFX bytes; value omitted.' }
if ($certificateBytes.Length -eq 0) { throw 'CertificateBase64 cannot be empty.' }
[Array]::Clear($certificateBytes, 0, $certificateBytes.Length)
Write-Output 'Deployment settings passed static validation. No cloud resources or database were changed.'
Write-Output 'Still verify PFX password/private key, SQL connectivity, Blob permissions and SMTP delivery on Azure.'
