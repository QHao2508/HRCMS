param(
    [Parameter(Mandatory = $true)][string]$ServiceUri,
    [Parameter(Mandatory = $true)][string]$Container,
    [switch]$UseAzureIdentity,
    [ValidateSet('AzureCli', 'Default')][string]$IdentityCredential = 'AzureCli'
)
$ErrorActionPreference = 'Stop'
$parsedUri = [Uri]$ServiceUri
if ($parsedUri.Scheme -ne 'https' -or !$parsedUri.Host.EndsWith('.blob.core.windows.net') -or $parsedUri.AbsolutePath -ne '/' -or $parsedUri.Query) { throw 'Use the HTTPS service URL, without a container path or SAS token.' }
if ($Container -notmatch '^(?!.*--)[a-z0-9][a-z0-9-]{1,61}[a-z0-9]$') { throw 'Invalid container name.' }
$backendProject = Join-Path $PSScriptRoot '../Horse_BackEnd/Horse_BackEnd.csproj'
$secretPointer = [IntPtr]::Zero
$secureConnection = $null
try {
    $connectionString = ''
    if (!$UseAzureIdentity) {
        $secureConnection = Read-Host 'Azure Storage connection string (Azure Portal > Access keys)' -AsSecureString
        $secretPointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secureConnection)
        $connectionString = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($secretPointer)
        $accountName = $parsedUri.Host.Split('.')[0]
        if ($connectionString -notmatch 'DefaultEndpointsProtocol=https' -or $connectionString -notmatch ('AccountName=' + [Regex]::Escape($accountName) + '(;|$)')) { throw 'Connection string must use HTTPS and match the specified account.' }
    }
    $settings = @{
        'Storage:Provider' = 'AzureBlob'; 'Storage:ServiceUri' = $parsedUri.AbsoluteUri
        'Storage:Container' = $Container; 'Storage:ConnectionString' = $connectionString
        'Storage:IdentityCredential' = $IdentityCredential
    } | ConvertTo-Json -Compress
    $settings | dotnet user-secrets set --project $backendProject
    if ($LASTEXITCODE -ne 0) { throw 'Could not save storage settings.' }
    Write-Host 'Azure Blob settings saved to User Secrets. Restart the API. Existing local files have not been migrated.'
}
finally {
    if ($secretPointer -ne [IntPtr]::Zero) { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($secretPointer) }
    if ($secureConnection) { $secureConnection.Dispose() }
    $connectionString = $null; $settings = $null
}
