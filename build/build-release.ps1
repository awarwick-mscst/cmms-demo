param(
    [Parameter(Mandatory=$true)]
    [string]$Version,

    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$publishDir = Join-Path $repoRoot "publish"
$appPublishDir = Join-Path $publishDir "app"
$frontendDir = Join-Path $repoRoot "frontend"
$apiDir = Join-Path $repoRoot "src" "CMMS.API"
$updaterDir = Join-Path $repoRoot "tools" "CmmsUpdater"
$buildDir = $PSScriptRoot

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  CMMS Release Build - v$Version" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Clean publish directory
if (Test-Path $publishDir) {
    Write-Host "`n[1/7] Cleaning previous publish output..."
    Remove-Item -Recurse -Force $publishDir
}
New-Item -ItemType Directory -Path $appPublishDir -Force | Out-Null

# Step 1: Build frontend
Write-Host "`n[2/7] Building frontend..."
Push-Location $frontendDir
try {
    npm ci
    if ($LASTEXITCODE -ne 0) { throw "npm ci failed" }

    npm run build
    if ($LASTEXITCODE -ne 0) { throw "npm run build failed" }
} finally {
    Pop-Location
}

# Step 2: Copy frontend build to wwwroot
Write-Host "`n[3/7] Copying frontend build to wwwroot..."
$wwwrootDir = Join-Path $apiDir "wwwroot"
$frontendBuildDir = Join-Path $frontendDir "build"

# Preserve uploads directory if it exists
$uploadsDir = Join-Path $wwwrootDir "uploads"
$uploadsExist = Test-Path $uploadsDir

if (Test-Path $wwwrootDir) {
    # Remove everything except uploads
    Get-ChildItem -Path $wwwrootDir -Exclude "uploads" | Remove-Item -Recurse -Force
}

if (Test-Path $frontendBuildDir) {
    Copy-Item -Path "$frontendBuildDir\*" -Destination $wwwrootDir -Recurse -Force
} else {
    throw "Frontend build directory not found: $frontendBuildDir"
}

# Step 3: Publish CMMS.API
Write-Host "`n[4/7] Publishing CMMS.API (win-x64, self-contained)..."
dotnet publish $apiDir `
    -c $Configuration `
    -r win-x64 `
    --self-contained `
    -p:Version=$Version `
    -p:AssemblyVersion="$Version.0" `
    -p:FileVersion="$Version.0" `
    -o $appPublishDir

if ($LASTEXITCODE -ne 0) { throw "dotnet publish CMMS.API failed" }

# Step 4: Publish CmmsUpdater
Write-Host "`n[5/7] Publishing CmmsUpdater (win-x64, self-contained, single file)..."
dotnet publish $updaterDir `
    -c $Configuration `
    -r win-x64 `
    --self-contained `
    -p:PublishSingleFile=true `
    -o $appPublishDir

if ($LASTEXITCODE -ne 0) { throw "dotnet publish CmmsUpdater failed" }

# Step 5: Copy production config template
Write-Host "`n[6/7] Copying production config template..."
$templatePath = Join-Path $buildDir "appsettings.Production.template.json"
if (Test-Path $templatePath) {
    Copy-Item $templatePath (Join-Path $appPublishDir "appsettings.Production.template.json")
}

# Step 6: Create zip archive
Write-Host "`n[7/7] Creating release archive..."
$zipName = "cmms-$Version-win-x64.zip"
$zipPath = Join-Path $publishDir $zipName

if (Test-Path $zipPath) { Remove-Item $zipPath }
Compress-Archive -Path "$appPublishDir\*" -DestinationPath $zipPath

# Compute SHA256
$hash = (Get-FileHash -Path $zipPath -Algorithm SHA256).Hash.ToLower()
$zipSize = (Get-Item $zipPath).Length
$zipSizeMB = [math]::Round($zipSize / 1MB, 2)

Write-Host "`n========================================" -ForegroundColor Green
Write-Host "  Build Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host "  Version:   $Version"
Write-Host "  Archive:   $zipPath"
Write-Host "  Size:      $zipSizeMB MB"
Write-Host "  SHA256:    $hash"
Write-Host "========================================" -ForegroundColor Green

# Output hash to file for CI usage
$hash | Out-File (Join-Path $publishDir "sha256.txt") -NoNewline
