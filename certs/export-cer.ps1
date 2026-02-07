$cert = Get-ChildItem -Path "Cert:\CurrentUser\My" | Where-Object { $_.Subject -eq "CN=localhost" -and $_.FriendlyName -eq "CMMS Dev Certificate" } | Select-Object -First 1
if ($cert) {
    Export-Certificate -Cert $cert -FilePath "C:\Users\aaron\source\claude\demo\certs\cmms-dev.cer" -Type CERT
    Write-Host "Certificate exported to cmms-dev.cer"
} else {
    Write-Host "Certificate not found"
}
