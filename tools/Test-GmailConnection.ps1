$ErrorActionPreference = 'Stop'
$secretPath = Join-Path ([Environment]::GetFolderPath('ApplicationData')) 'Microsoft/UserSecrets/horseclub-backend/secrets.json'
$settings = Get-Content -Raw -LiteralPath $secretPath | ConvertFrom-Json
if ($settings.'Email:Provider' -ne 'Smtp' -or $settings.'Email:Smtp:Host' -ne 'smtp.gmail.com') { throw 'Configure Email:Provider and Email:Smtp:* with dotnet user-secrets first.' }
if ([string]::IsNullOrWhiteSpace($settings.'Email:Smtp:Username') -or [string]::IsNullOrWhiteSpace($settings.'Email:Smtp:Password')) { throw 'SMTP credentials are missing.' }

function Read-SmtpReply($Reader) {
    do {
        $line = $Reader.ReadLine()
        if ($null -eq $line -or $line.Length -lt 3) { throw 'SMTP connection ended unexpectedly.' }
    } while ($line.Length -gt 3 -and $line[3] -eq '-')
    return $line
}
function Send-SmtpCommand($Writer, $Reader, [string]$Command, [string]$Expected) {
    $Writer.WriteLine($Command)
    $reply = Read-SmtpReply $Reader
    if (!$reply.StartsWith($Expected)) {
        # Server status is useful; never print the command (AUTH may contain credentials).
        throw "Gmail SMTP returned: $reply"
    }
    return $reply
}

$client = New-Object Net.Sockets.TcpClient
$tls = $null
$reader = $null
$writer = $null
try {
    $connect = $client.ConnectAsync('smtp.gmail.com', 587)
    if (!$connect.Wait(15000)) { throw 'Timed out connecting to smtp.gmail.com:587.' }
    $network = $client.GetStream()
    $network.ReadTimeout = 15000; $network.WriteTimeout = 15000
    $reader = [IO.StreamReader]::new($network, [Text.Encoding]::ASCII, $false, 1024, $true)
    $writer = [IO.StreamWriter]::new($network, [Text.Encoding]::ASCII, 1024, $true)
    $writer.NewLine = "`r`n"; $writer.AutoFlush = $true
    $greeting = Read-SmtpReply $reader
    if (!$greeting.StartsWith('220')) { throw 'Unexpected SMTP greeting.' }
    Send-SmtpCommand $writer $reader 'EHLO hrcms.local' '250' | Out-Null
    Send-SmtpCommand $writer $reader 'STARTTLS' '220' | Out-Null
    $reader.Dispose(); $writer.Dispose()
    $tls = [Net.Security.SslStream]::new($network, $false)
    # Default certificate validation is required. Never bypass Gmail certificates.
    $tls.AuthenticateAsClient('smtp.gmail.com')
    $tls.ReadTimeout = 15000; $tls.WriteTimeout = 15000
    $reader = [IO.StreamReader]::new($tls, [Text.Encoding]::ASCII, $false, 1024, $true)
    $writer = [IO.StreamWriter]::new($tls, [Text.Encoding]::ASCII, 1024, $true)
    $writer.NewLine = "`r`n"; $writer.AutoFlush = $true
    Send-SmtpCommand $writer $reader 'EHLO hrcms.local' '250' | Out-Null
    Send-SmtpCommand $writer $reader 'AUTH LOGIN' '334' | Out-Null
    Send-SmtpCommand $writer $reader ([Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($settings.'Email:Smtp:Username'))) '334' | Out-Null
    Send-SmtpCommand $writer $reader ([Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($settings.'Email:Smtp:Password'.Replace(' ', '')))) '235' | Out-Null
    Write-Host 'PASS: Gmail STARTTLS and SMTP authentication succeeded. No email was sent.'
    Send-SmtpCommand $writer $reader 'QUIT' '221' | Out-Null
}
finally {
    if ($reader) { $reader.Dispose() }
    if ($writer) { $writer.Dispose() }
    if ($tls) { $tls.Dispose() }
    $client.Dispose(); $settings = $null
}
