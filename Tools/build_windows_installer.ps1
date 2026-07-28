param(
    [string]$ProjectRoot = "",
    [string]$UnityExe = "",
    [string]$VersionTag = "",
    [switch]$KeepTemp
)

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

function Write-Step([string]$Message) {
    Write-Host ""
    Write-Host "== $Message ==" -ForegroundColor Cyan
}

function Resolve-ProjectRoot {
    if (-not [string]::IsNullOrWhiteSpace($ProjectRoot)) {
        return (Resolve-Path -LiteralPath $ProjectRoot).Path
    }
    return (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")).Path
}

function Find-UnityExe([string]$Root) {
    if (-not [string]::IsNullOrWhiteSpace($UnityExe)) {
        if (-not (Test-Path -LiteralPath $UnityExe)) {
            throw "Unity executable not found: $UnityExe"
        }
        return (Resolve-Path -LiteralPath $UnityExe).Path
    }

    $versionFile = Join-Path $Root "ProjectSettings\ProjectVersion.txt"
    if (Test-Path -LiteralPath $versionFile) {
        $versionLine = Get-Content -LiteralPath $versionFile -Encoding UTF8 | Where-Object { $_ -like "m_EditorVersion:*" } | Select-Object -First 1
        if ($versionLine) {
            $version = ($versionLine -replace "m_EditorVersion:\s*", "").Trim()
            $preferred = Join-Path "C:\Program Files\Unity\Hub\Editor" "$version\Editor\Unity.exe"
            if (Test-Path -LiteralPath $preferred) {
                return $preferred
            }
        }
    }

    $editorsRoot = "C:\Program Files\Unity\Hub\Editor"
    $fallback = Get-ChildItem -LiteralPath $editorsRoot -Directory -ErrorAction SilentlyContinue |
        Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName "Editor\Unity.exe") } |
        Sort-Object Name -Descending |
        Select-Object -First 1
    if ($fallback) {
        return (Join-Path $fallback.FullName "Editor\Unity.exe")
    }

    throw "Unity executable was not found under $editorsRoot."
}

function Remove-SafeDirectory([string]$Path, [string]$ExpectedPath) {
    if (-not (Test-Path -LiteralPath $Path)) { return }
    $resolved = (Resolve-Path -LiteralPath $Path).Path.TrimEnd("\")
    $expected = $ExpectedPath.TrimEnd("\")
    if ($resolved -ne $expected) {
        throw "Refusing to remove unexpected path: $resolved"
    }
    Remove-Item -LiteralPath $resolved -Recurse -Force
}

function Assert-PackageContents([string]$ZipPath, [string]$CheckRoot) {
    if (Test-Path -LiteralPath $CheckRoot) {
        Remove-Item -LiteralPath $CheckRoot -Recurse -Force
    }
    New-Item -ItemType Directory -Path $CheckRoot | Out-Null
    Expand-Archive -LiteralPath $ZipPath -DestinationPath $CheckRoot -Force

    $required = @("MingLu.exe", "UnityPlayer.dll", "MingLu_Data", "MonoBleedingEdge")
    $missing = @()
    foreach ($item in $required) {
        if (-not (Test-Path -LiteralPath (Join-Path $CheckRoot $item))) {
            $missing += $item
        }
    }
    if ($missing.Count -gt 0) {
        throw "Package missing: $($missing -join ', ')"
    }
}

$root = Resolve-ProjectRoot
if ([string]::IsNullOrWhiteSpace($VersionTag)) {
    $VersionTag = Get-Date -Format "yyyyMMdd-HHmm"
}

$unity = Find-UnityExe $root
$buildDir = Join-Path $root "Builds\Windows"
$installerDir = Join-Path $root "Builds\Installers"
$logDir = Join-Path $root "Builds\Logs"
$tmpRoot = Join-Path (Split-Path -Parent $root) ".codex-unity-package-$VersionTag"
$checkRoot = Join-Path (Split-Path -Parent $root) ".codex-package-check-$VersionTag"

Write-Host "ProjectRoot: $root"
Write-Host "UnityExe: $unity"
Write-Host "VersionTag: $VersionTag"

New-Item -ItemType Directory -Path $installerDir -Force | Out-Null
New-Item -ItemType Directory -Path $logDir -Force | Out-Null

try {
    Write-Step "Prepare temporary Unity project"
    if (Test-Path -LiteralPath $tmpRoot) {
        Remove-SafeDirectory $tmpRoot $tmpRoot
    }
    New-Item -ItemType Directory -Path $tmpRoot | Out-Null

    $excludeDirs = @("Library", "Temp", "Logs", "obj", "Build", "Builds", ".git")
    $roboArgs = @($root, $tmpRoot, "/E", "/XD") + $excludeDirs + @("/NFL", "/NDL", "/NJH", "/NJS", "/NP")
    & robocopy.exe @roboArgs | Out-Null
    $copyExit = $LASTEXITCODE
    if ($copyExit -gt 7) {
        throw "robocopy failed with exit code $copyExit"
    }

    Write-Step "Run Unity release build"
    $unityLog = Join-Path $logDir "unity-build-$VersionTag.log"
    $unityErrLog = Join-Path $logDir "unity-build-$VersionTag.err.log"
    if (Test-Path -LiteralPath $unityLog) { Remove-Item -LiteralPath $unityLog -Force }
    if (Test-Path -LiteralPath $unityErrLog) { Remove-Item -LiteralPath $unityErrLog -Force }

    $unityArgs = @(
        "-batchmode",
        "-nographics",
        "-quit",
        "-projectPath", $tmpRoot,
        "-executeMethod", "MingLuEditorTools.BuildWindows",
        "-logFile", "-"
    )
    $unityProc = Start-Process -FilePath $unity -ArgumentList $unityArgs -NoNewWindow -Wait -PassThru -RedirectStandardOutput $unityLog -RedirectStandardError $unityErrLog
    if ($unityProc.ExitCode -ne 0) {
        if (Test-Path -LiteralPath $unityLog) {
            Get-Content -LiteralPath $unityLog -Tail 120 -Encoding UTF8
        }
        throw "Unity build failed with exit code $($unityProc.ExitCode). See $unityLog"
    }

    $tmpBuildDir = Join-Path $tmpRoot "Builds\Windows"
    if (-not (Test-Path -LiteralPath (Join-Path $tmpBuildDir "MingLu.exe"))) {
        throw "Unity build did not create MingLu.exe."
    }

    Write-Step "Copy build output"
    Remove-SafeDirectory $buildDir $buildDir
    New-Item -ItemType Directory -Path (Split-Path -Parent $buildDir) -Force | Out-Null
    & robocopy.exe $tmpBuildDir $buildDir /E /NFL /NDL /NJH /NJS /NP | Out-Null
    $buildCopyExit = $LASTEXITCODE
    if ($buildCopyExit -gt 7) {
        throw "Build copy failed with exit code $buildCopyExit"
    }

    Write-Step "Create installer and zip package"
    $packager = Join-Path $root "Tools\package_windows_installer.ps1"
    Write-Host "Packager: $packager"
    Write-Host "BuildDir: $buildDir"
    Write-Host "InstallerDir: $installerDir"
    & $packager -BuildDir $buildDir -InstallerDir $installerDir -VersionTag $VersionTag

    $zipPackagePath = Join-Path $installerDir "MingLu_Windows_$VersionTag.zip"
    $setupPackagePath = Join-Path $installerDir "MingLu_Setup_$VersionTag.exe"
    Write-Host "ZipPath: $zipPackagePath"
    Write-Host "SetupPath: $setupPackagePath"
    if (-not (Test-Path -LiteralPath $zipPackagePath)) {
        throw "Zip package was not created: $zipPackagePath"
    }
    if (-not (Test-Path -LiteralPath $setupPackagePath)) {
        throw "Installer was not created: $setupPackagePath"
    }

    Write-Step "Verify package contents"
    Assert-PackageContents -ZipPath $zipPackagePath -CheckRoot $checkRoot
    $zipHash = (Get-FileHash -LiteralPath $zipPackagePath -Algorithm SHA256).Hash
    $setupHash = (Get-FileHash -LiteralPath $setupPackagePath -Algorithm SHA256).Hash

    $latest = Join-Path $installerDir "latest_windows_installer.txt"
    @(
        "VersionTag: $VersionTag",
        "Installer: $setupPackagePath",
        "InstallerSHA256: $setupHash",
        "Zip: $zipPackagePath",
        "ZipSHA256: $zipHash",
        "UnityLog: $unityLog"
    ) | Set-Content -LiteralPath $latest -Encoding UTF8

    Write-Step "Done"
    Write-Host "Installer: $setupPackagePath" -ForegroundColor Green
    Write-Host "Zip: $zipPackagePath" -ForegroundColor Green
    Write-Host "Latest manifest: $latest" -ForegroundColor Green
}
finally {
    if (-not $KeepTemp) {
        if (Test-Path -LiteralPath $checkRoot) {
            Remove-SafeDirectory $checkRoot $checkRoot
        }
        if (Test-Path -LiteralPath $tmpRoot) {
            Remove-SafeDirectory $tmpRoot $tmpRoot
        }
    }
}
