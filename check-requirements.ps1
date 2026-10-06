#Requires -Version 5.1
<#
.SYNOPSIS
    Checks all requirements for AIMPYouTube C# plugin development using VS Code.

.DESCRIPTION
    Verifies that all necessary tools, SDKs, and extensions are installed
    for developing the AIMPYouTube plugin with C# and AIMP DotNet SDK.

.NOTES
    Run this script in PowerShell:
    .\check-requirements.ps1
#>

$ErrorActionPreference = "Continue"

# ── Helpers ──────────────────────────────────────────────────────────────

function Write-Status {
    param(
        [string]$Name,
        [bool]$Passed,
        [string]$Detail = "",
        [string]$Fix = ""
    )
    if ($Passed) {
        Write-Host "  [OK]   " -ForegroundColor Green -NoNewline
        Write-Host "$Name" -NoNewline
        if ($Detail) { Write-Host " ($Detail)" -ForegroundColor DarkGray } else { Write-Host "" }
    } else {
        Write-Host "  [MISS] " -ForegroundColor Red -NoNewline
        Write-Host "$Name" -ForegroundColor Yellow
        if ($Fix) {
            Write-Host "         -> $Fix" -ForegroundColor DarkCyan
        }
    }
    return $Passed
}

function Test-CommandExists {
    param([string]$Command)
    try {
        $null = Get-Command $Command -ErrorAction Stop
        return $true
    } catch {
        return $false
    }
}

# ── Start ────────────────────────────────────────────────────────────────

Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "  AIMPYouTube C# Plugin - Requirements Check" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host ""

$totalChecks = 0
$passedChecks = 0

# ─────────────────────────────────────────────────────────────────────────
# 1. .NET SDK
# ─────────────────────────────────────────────────────────────────────────
Write-Host "[1/6] .NET SDK" -ForegroundColor White
$totalChecks++

$dotnetInstalled = Test-CommandExists "dotnet"
if ($dotnetInstalled) {
    $dotnetVersion = (dotnet --version 2>$null)
    $sdks = dotnet --list-sdks 2>$null
    $hasSdk = $null -ne $sdks -and $sdks.Count -gt 0

    if ($hasSdk) {
        $result = Write-Status ".NET SDK" $true "dotnet $dotnetVersion"
        if ($result) { $passedChecks++ }

        # Show installed SDKs
        foreach ($sdk in $sdks) {
            Write-Host "         SDK: $sdk" -ForegroundColor DarkGray
        }
    } else {
        Write-Status ".NET SDK" $false "" "Install from: https://dotnet.microsoft.com/download"
    }
} else {
    Write-Status ".NET SDK" $false "" "Install from: https://dotnet.microsoft.com/download (pilih SDK, bukan Runtime)"
}

Write-Host ""

# ─────────────────────────────────────────────────────────────────────────
# 2. .NET Framework 4.8 Developer/Targeting Pack
# ─────────────────────────────────────────────────────────────────────────
Write-Host "[2/6] .NET Framework 4.8" -ForegroundColor White
$totalChecks++

# Check for .NET Framework 4.8 via registry
$ndpKey = "HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full"
$net48Installed = $false
$net48Version = ""

if (Test-Path $ndpKey) {
    $release = (Get-ItemProperty $ndpKey -Name Release -ErrorAction SilentlyContinue).Release
    # 528040 = .NET Framework 4.8 on Windows 10 May 2019+
    # 528049 = .NET Framework 4.8 on other OS
    if ($release -ge 528040) {
        $net48Installed = $true
        $net48Version = "4.8 (Release $release)"
    }
}

# Check for Developer/Targeting Pack (needed to build against net48)
$targetingPackPath = "${env:ProgramFiles(x86)}\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8"
$hasTargetingPack = Test-Path $targetingPackPath

$result = Write-Status ".NET Framework 4.8 Runtime" $net48Installed $net48Version "Install from: https://dotnet.microsoft.com/download/dotnet-framework/net48"
if ($result) { $passedChecks++ }

$totalChecks++
$result = Write-Status ".NET Framework 4.8 Developer Pack" $hasTargetingPack "" "Install Developer Pack from: https://dotnet.microsoft.com/download/dotnet-framework/net48"
if ($result) { $passedChecks++ }

Write-Host ""

# ─────────────────────────────────────────────────────────────────────────
# 3. VS Code
# ─────────────────────────────────────────────────────────────────────────
Write-Host "[3/6] Visual Studio Code" -ForegroundColor White
$totalChecks++

$codeInstalled = Test-CommandExists "code"
if ($codeInstalled) {
    $codeVersion = (code --version 2>$null | Select-Object -First 1)
    $result = Write-Status "VS Code" $true "v$codeVersion"
    if ($result) { $passedChecks++ }
} else {
    Write-Status "VS Code" $false "" "Install from: https://code.visualstudio.com/"
}

Write-Host ""

# ─────────────────────────────────────────────────────────────────────────
# 4. VS Code Extensions
# ─────────────────────────────────────────────────────────────────────────
Write-Host "[4/6] VS Code Extensions" -ForegroundColor White

$requiredExtensions = @(
    @{ Id = "ms-dotnettools.csharp";       Name = "C# (OmniSharp)";   Fix = "code --install-extension ms-dotnettools.csharp" },
    @{ Id = "ms-dotnettools.csdevkit";     Name = "C# Dev Kit";       Fix = "code --install-extension ms-dotnettools.csdevkit" },
    @{ Id = "ms-dotnettools.vscode-dotnet-runtime"; Name = ".NET Runtime";  Fix = "code --install-extension ms-dotnettools.vscode-dotnet-runtime" }
)

if ($codeInstalled) {
    $installedExts = code --list-extensions 2>$null

    foreach ($ext in $requiredExtensions) {
        $totalChecks++
        $isInstalled = $installedExts -contains $ext.Id
        $result = Write-Status "Extension: $($ext.Name)" $isInstalled "" $ext.Fix
        if ($result) { $passedChecks++ }
    }
} else {
    foreach ($ext in $requiredExtensions) {
        $totalChecks++
        Write-Status "Extension: $($ext.Name)" $false "" "Install VS Code first"
    }
}

Write-Host ""

# ─────────────────────────────────────────────────────────────────────────
# 5. AIMP Player
# ─────────────────────────────────────────────────────────────────────────
Write-Host "[5/6] AIMP Player" -ForegroundColor White
$totalChecks++

$aimpPaths = @(
    "${env:ProgramFiles}\AIMP\AIMP.exe",
    "${env:ProgramFiles(x86)}\AIMP\AIMP.exe",
    "$env:LOCALAPPDATA\AIMP\AIMP.exe"
)

$aimpFound = $false
$aimpExePath = ""
$aimpPluginDir = ""

foreach ($p in $aimpPaths) {
    if (Test-Path $p) {
        $aimpFound = $true
        $aimpExePath = $p
        $aimpPluginDir = Join-Path (Split-Path $p) "Plugins"
        break
    }
}

# Also search registry
if (-not $aimpFound) {
    $regPaths = @(
        "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\*",
        "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*",
        "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\*"
    )
    foreach ($regPath in $regPaths) {
        $entries = Get-ItemProperty $regPath -ErrorAction SilentlyContinue | Where-Object { $_.DisplayName -like "*AIMP*" }
        if ($entries) {
            $installLoc = $entries | Select-Object -First 1 -ExpandProperty InstallLocation -ErrorAction SilentlyContinue
            if ($installLoc -and (Test-Path (Join-Path $installLoc "AIMP.exe"))) {
                $aimpFound = $true
                $aimpExePath = Join-Path $installLoc "AIMP.exe"
                $aimpPluginDir = Join-Path $installLoc "Plugins"
                break
            }
        }
    }
}

if ($aimpFound) {
    $aimpVer = (Get-Item $aimpExePath).VersionInfo.ProductVersion
    $result = Write-Status "AIMP Player" $true "v$aimpVer - $aimpExePath"
    if ($result) { $passedChecks++ }

    Write-Host "         Plugins folder: $aimpPluginDir" -ForegroundColor DarkGray
} else {
    Write-Status "AIMP Player" $false "" "Install from: https://www.aimp.ru/?do=download"
}

Write-Host ""

# ─────────────────────────────────────────────────────────────────────────
# 6. Git (Optional)
# ─────────────────────────────────────────────────────────────────────────
Write-Host "[6/6] Git (Opsional)" -ForegroundColor White
$totalChecks++

$gitInstalled = Test-CommandExists "git"
if ($gitInstalled) {
    $gitVersion = (git --version 2>$null)
    $result = Write-Status "Git" $true $gitVersion
    if ($result) { $passedChecks++ }
} else {
    Write-Status "Git" $false "" "Install from: https://git-scm.com/download/win (opsional, tapi disarankan)"
}

Write-Host ""

# ─────────────────────────────────────────────────────────────────────────
# Summary
# ─────────────────────────────────────────────────────────────────────────
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "  HASIL: $passedChecks / $totalChecks checks passed" -ForegroundColor $(if ($passedChecks -eq $totalChecks) { "Green" } else { "Yellow" })
Write-Host "============================================================" -ForegroundColor Cyan

if ($passedChecks -eq $totalChecks) {
    Write-Host ""
    Write-Host "  Semua requirements sudah terpenuhi!" -ForegroundColor Green
    Write-Host "  Kamu siap mulai development." -ForegroundColor Green
    Write-Host ""

    if ($aimpFound) {
        Write-Host "  Quick Start:" -ForegroundColor White
        Write-Host "    1. cd d:\Project\AIMPYouTube" -ForegroundColor DarkGray
        Write-Host "    2. dotnet new classlib -n AIMPYouTube.CSharp -f net48" -ForegroundColor DarkGray
        Write-Host "    3. cd AIMPYouTube.CSharp" -ForegroundColor DarkGray
        Write-Host "    4. dotnet add package AimpSDK" -ForegroundColor DarkGray
        Write-Host "    5. code ." -ForegroundColor DarkGray
    }
} else {
    $missing = $totalChecks - $passedChecks
    Write-Host ""
    Write-Host "  Ada $missing item yang belum ter-install." -ForegroundColor Yellow
    Write-Host "  Install yang bertanda [MISS] lalu jalankan script ini lagi." -ForegroundColor Yellow
}

Write-Host ""
