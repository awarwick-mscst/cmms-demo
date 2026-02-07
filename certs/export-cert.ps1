$pwd = ConvertTo-SecureString -String "cmms2024" -Force -AsPlainText
$cert = Get-ChildItem -Path "Cert:\CurrentUser\My" | Where-Object { $_.Subject -eq "CN=localhost" -and $_.FriendlyName -eq "CMMS Dev Certificate" } | Select-Object -First 1
if ($cert) {
    Export-PfxCertificate -Cert $cert -FilePath "C:\Users\aaron\source\claude\demo\certs\cmms-dev.pfx" -Password $pwd
    Write-Host "Certificate exported successfully"
} else {
    Write-Host "Certificate not found"
}
