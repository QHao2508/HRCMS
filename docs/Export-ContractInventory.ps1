param(
    [string]$InputPath = (Join-Path $PSScriptRoot 'contracts/openapi.current.json'),
    [string]$OutputPath = (Join-Path $PSScriptRoot 'contracts/api-inventory.current.csv'),
    [string]$SummaryPath = (Join-Path $PSScriptRoot 'contracts/openapi-audit.current.json')
)

$ErrorActionPreference = 'Stop'
$document = Get-Content -LiteralPath $InputPath -Raw | ConvertFrom-Json -AsHashtable
$verbs = @('get', 'post', 'put', 'patch', 'delete')
$rows = foreach ($path in $document.paths.Keys) {
    foreach ($verb in $verbs) {
        $operation = $document.paths[$path][$verb]
        if ($null -eq $operation) { continue }
        $requestTypes = @()
        $requestSchemas = @()
        if ($operation.requestBody -and $operation.requestBody.content) {
            foreach ($media in $operation.requestBody.content.Keys) {
                $requestTypes += $media
                $requestSchemas += ($operation.requestBody.content[$media].schema | ConvertTo-Json -Depth 30 -Compress)
            }
        }
        $responseSchemas = @()
        $emptyResponse = $false
        foreach ($status in $operation.responses.Keys) {
            $response = $operation.responses[$status]
            if ($response.content) {
                foreach ($media in $response.content.Keys) {
                    $schema = $response.content[$media].schema
                    $responseSchemas += "$status ${media}: " + ($schema | ConvertTo-Json -Depth 30 -Compress)
                    if ($schema -is [System.Collections.IDictionary] -and $schema.Count -eq 0) { $emptyResponse = $true }
                }
            }
        }
        $security = if ($operation.ContainsKey('security')) { $operation.security } else { $document.security }
        $queryParameters = @($operation.parameters | Where-Object { $_.in -eq 'query' } | ForEach-Object { $_.name })
        [pscustomobject]@{
            Method = $verb.ToUpperInvariant()
            Path = $path
            Module = @($operation.tags) -join '; '
            QueryParameters = $queryParameters -join '; '
            RequestContentTypes = $requestTypes -join '; '
            RequestSchemas = $requestSchemas -join '; '
            DeclaredStatuses = @($operation.responses.Keys | Sort-Object) -join '; '
            DeclaredResponseSchemas = $responseSchemas -join '; '
            HasEmptyResponseSchema = $emptyResponse
            HasSecurityRequirement = ($null -ne $security -and @($security).Count -gt 0)
        }
    }
}
$rows = @($rows | Sort-Object Path, Method)
if ($rows.Count -ne @($rows | Select-Object Method, Path -Unique).Count) {
    throw 'Duplicate method/path in contract inventory.'
}
$rows | Export-Csv -LiteralPath $OutputPath -NoTypeInformation -Encoding utf8
$uploadPaths = @(
    '/api/registrations/{registrationId}/attachments',
    '/api/care/incidents/{incidentId}/photos'
)
$uploadGaps = @($rows | Where-Object { $_.Method -eq 'POST' -and $_.Path -in $uploadPaths -and $_.RequestContentTypes -notmatch 'multipart/form-data' })
$summary = [ordered]@{
    Source = 'Backend Development OpenAPI; metadata audit, not workflow test'
    PathCount = $document.paths.Count
    OperationCount = $rows.Count
    SchemaCount = $document.components.schemas.Count
    OperationsDeclaringOnly200 = @($rows | Where-Object DeclaredStatuses -eq '200').Count
    OperationsWithEmptyResponseSchema = @($rows | Where-Object HasEmptyResponseSchema).Count
    OperationsWithoutResponseContent = @($rows | Where-Object DeclaredResponseSchemas -eq '').Count
    OperationsWithSecurityRequirement = @($rows | Where-Object HasSecurityRequirement).Count
    SecuritySchemeCount = if ($document.components.securitySchemes) { $document.components.securitySchemes.Count } else { 0 }
    UploadOperationsWithoutMultipart = @($uploadGaps | Select-Object Method, Path)
}
$summary | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $SummaryPath -Encoding utf8
$summary | ConvertTo-Json -Depth 10 -Compress
