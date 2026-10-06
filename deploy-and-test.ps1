#Requires -Version 5.1
<#
.SYNOPSIS
    Builds, installs, and launches the AIMPYouTube C# plugin for testing.
#>

param(
    [string]$Configuration = "Release",
    [object]$LaunchAimp = $true,
    [object]$DownloadYtDlp = $true
)

$ErrorActionPreference = "Stop"
$shouldLaunch = ($LaunchAimp -eq $true -or "$LaunchAimp".ToLower() -eq "true" -or "$LaunchAimp" -eq "1")
# $shouldDownload = ($DownloadYtDlp -eq $true -or "$DownloadYtDlp".ToLower() -eq "true" -or "$DownloadYtDlp" -eq "1")
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectDir = $ScriptDir
$ProjectFile = Join-Path $ProjectDir "AIMPYouTube.csproj"

Write-Host "====================================================" -ForegroundColor Cyan
Write-Host "   AIMP YouTube Plugin (C# .NET) - Deploy & Test   " -ForegroundColor Cyan
Write-Host "====================================================" -ForegroundColor Cyan

# 1. Check & Close AIMP if running
$aimpProc = Get-Process -Name "AIMP" -ErrorAction SilentlyContinue
if ($aimpProc) {
    Write-Host "`n[1/5] Closing running AIMP process..." -ForegroundColor Yellow
    try {
        $aimpProc | Stop-Process -Force -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 1
    } catch { }
} else {
    Write-Host "`n[1/5] AIMP is not currently running." -ForegroundColor Green
}

# 2. Build C# Project
Write-Host "`n[2/5] Building C# project ($Configuration)..." -ForegroundColor Yellow
dotnet build $ProjectFile -c $Configuration --nologo
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed! Please check compilation errors above." -ForegroundColor Red
    exit 1
}
Write-Host "  -> Build succeeded!" -ForegroundColor Green

# 3. Locate Target Plugin Directories
$BinDir = Join-Path $ProjectDir "bin\$Configuration\net48"
$DistDir = Join-Path $ProjectDir "dist"
$PackageDir = Join-Path $DistDir "AIMPYouTube"
$LangsDir = Join-Path $ProjectDir "Langs"
$ProgramFilesPluginDir = "C:\Program Files\AIMP\Plugins\AIMPYouTube"
$AppDataPluginDir = Join-Path $env:APPDATA "AIMP\Plugins\AIMPYouTube"

# 4. Build Distribution Package (Zip & Aimppack)
Write-Host "`n[3/6] Packaging plugin archive (Zip / Aimppack)..." -ForegroundColor Yellow
if (Test-Path $DistDir) { Remove-Item $DistDir -Recurse -Force -ErrorAction SilentlyContinue }
New-Item -ItemType Directory -Path "$PackageDir\Langs" -Force | Out-Null

Copy-Item "$BinDir\aimp_dotnet.dll" -Destination "$PackageDir\AIMPYouTube.dll" -Force
Copy-Item "$BinDir\AIMPYouTube.dll" -Destination "$PackageDir\AIMPYouTube_plugin.dll" -Force
Copy-Item "$BinDir\AIMP.SDK.dll" -Destination "$PackageDir\AIMP.SDK.dll" -Force
Copy-Item "$BinDir\Newtonsoft.Json.dll" -Destination "$PackageDir\Newtonsoft.Json.dll" -Force
if (Test-Path "$BinDir\AIMPYouTube.pdb") {
    Copy-Item "$BinDir\AIMPYouTube.pdb" -Destination "$PackageDir\AIMPYouTube_plugin.pdb" -Force
}
if (Test-Path $LangsDir) {
    Copy-Item "$LangsDir\*" -Destination "$PackageDir\Langs\" -Force
}

# Check yt-dlp
$ytdlp = Get-Command yt-dlp.exe -ErrorAction SilentlyContinue
if ($ytdlp) {
    Copy-Item $ytdlp.Source -Destination "$PackageDir\yt-dlp.exe" -Force
} elseif (Test-Path "$AppDataPluginDir\yt-dlp.exe") {
    Copy-Item "$AppDataPluginDir\yt-dlp.exe" -Destination "$PackageDir\yt-dlp.exe" -Force
}

$ZipFile = Join-Path $DistDir "AIMPYouTube.zip"
$AimppackFile = Join-Path $DistDir "AIMPYouTube.aimppack"
Compress-Archive -Path $PackageDir -DestinationPath $ZipFile -Force
Copy-Item $ZipFile $AimppackFile -Force
Write-Host "  -> Created: dist\AIMPYouTube.zip & dist\AIMPYouTube.aimppack" -ForegroundColor Green

# 5. Deploy Plugin Files
Write-Host "`n[4/6] Deploying plugin files..." -ForegroundColor Yellow

# Always deploy to AppData
try {
    if (-not (Test-Path "$AppDataPluginDir\Langs")) {
        New-Item -ItemType Directory -Path "$AppDataPluginDir\Langs" -Force | Out-Null
    }
    Copy-Item "$PackageDir\*" -Destination $AppDataPluginDir -Recurse -Force
    Write-Host "  -> Deployed to AppData: $AppDataPluginDir" -ForegroundColor Green
} catch {
    Write-Host "  -> AppData deploy notice: $($_.Exception.Message)" -ForegroundColor Yellow
}

# Check if Program Files requires elevation
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if ($isAdmin) {
    try {
        if (-not (Test-Path "$ProgramFilesPluginDir\Langs")) {
            New-Item -ItemType Directory -Path "$ProgramFilesPluginDir\Langs" -Force | Out-Null
        }
        Copy-Item "$PackageDir\*" -Destination $ProgramFilesPluginDir -Recurse -Force
        Write-Host "  -> Deployed to Program Files: $ProgramFilesPluginDir" -ForegroundColor Green
    } catch {
        Write-Host "  -> Program Files deploy failed: $($_.Exception.Message)" -ForegroundColor Yellow
    }
} else {
    Write-Host "  -> Note: To install directly into Program Files, run PowerShell as Administrator or use the generated .aimppack / .zip file in AIMP." -ForegroundColor DarkCyan
}

# 6. Launch AIMP
$aimpExe = "C:\Program Files\AIMP\AIMP.exe"
if ($shouldLaunch -and (Test-Path $aimpExe)) {
    Write-Host "`n[5/6] Launching AIMP Player..." -ForegroundColor Yellow
    Start-Process -FilePath $aimpExe
    Write-Host "  -> AIMP launched!" -ForegroundColor Green
}

Write-Host "`n====================================================" -ForegroundColor Green
Write-Host "   DEPLOYMENT COMPLETE! Plugin ready for testing.   " -ForegroundColor Green
Write-Host "====================================================" -ForegroundColor Green
Write-Host "Cara Install & Test di AIMP:" -ForegroundColor White
Write-Host "1. Buka AIMP > Preferences (Ctrl+P) > Plugins" -ForegroundColor Gray
Write-Host "2. Jika belum muncul di list, klik 'Install' di pojok kiri bawah menu Plugins," -ForegroundColor Gray
Write-Host "   lalu pilih file: $AimppackFile (atau .zip)" -ForegroundColor Gray
Write-Host "3. Pastikan centang 'YouTube Support' aktif!" -ForegroundColor Gray
Write-Host "4. Klik tombol '+' di playlist > Pilih 'YouTube URL...' > Paste link YouTube!" -ForegroundColor Gray
