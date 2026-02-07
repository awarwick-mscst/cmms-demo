$cert = Get-ChildItem -Path "Cert:\CurrentUser\My" | Where-Object { $_.Subject -eq "CN=localhost" -and $_.FriendlyName -eq "CMMS Dev Certificate" } | Select-Object -First 1
if ($cert) {
    $pemContent = "-----BEGIN CERTIFICATE-----`r`n"
    $pemContent += [Convert]::ToBase64String($cert.RawData, [Base64FormattingOptions]::InsertLineBreaks)
    $pemContent += "`r`n-----END CERTIFICATE-----"
    $pemContent | Out-File -FilePath "C:\Users\aaron\source\claude\demo\certs\cmms-dev.crt" -Encoding ASCII
    Write-Host "Certificate exported to cmms-dev.crt"
} else {
    Write-Host "Certificate not found"
}
