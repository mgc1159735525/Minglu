param(
    [string]$BuildDir = "D:\paotuan\MingLuUnity\Builds\Windows",
    [string]$InstallerDir = "D:\paotuan\MingLuUnity\Builds\Installers",
    [string]$VersionTag = ""
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($VersionTag)) {
    $VersionTag = Get-Date -Format "yyyyMMdd-HHmm"
}

$buildFullPath = (Resolve-Path -LiteralPath $BuildDir).Path
$installerFullPath = if (Test-Path -LiteralPath $InstallerDir) {
    (Resolve-Path -LiteralPath $InstallerDir).Path
} else {
    (New-Item -ItemType Directory -Path $InstallerDir).FullName
}

$exePath = Join-Path $buildFullPath "MingLu.exe"
if (-not (Test-Path -LiteralPath $exePath)) {
    throw "Build output is missing MingLu.exe: $exePath"
}

$packageBase = "MingLu_Windows_$VersionTag"
$zipPath = Join-Path $installerFullPath "$packageBase.zip"
$setupPath = Join-Path $installerFullPath "MingLu_Setup_$VersionTag.exe"
$stagingPath = Join-Path $installerFullPath "_installer_staging_$VersionTag"

if (Test-Path -LiteralPath $zipPath) { Remove-Item -LiteralPath $zipPath -Force }
if (Test-Path -LiteralPath $setupPath) { Remove-Item -LiteralPath $setupPath -Force }
if (Test-Path -LiteralPath $stagingPath) { Remove-Item -LiteralPath $stagingPath -Recurse -Force }

Compress-Archive -Path (Join-Path $buildFullPath "*") -DestinationPath $zipPath -CompressionLevel Optimal -Force

New-Item -ItemType Directory -Path $stagingPath | Out-Null
Copy-Item -LiteralPath $zipPath -Destination (Join-Path $stagingPath "MingLu_Windows.zip") -Force

$installCmd = @'
@echo off
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0install.ps1"
exit /b %ERRORLEVEL%
'@
Set-Content -LiteralPath (Join-Path $stagingPath "install.cmd") -Value $installCmd -Encoding ASCII

$installPs1 = @'
$ErrorActionPreference = "Stop"
$installRoot = Join-Path $env:LOCALAPPDATA "MingLu"
$zip = Join-Path $PSScriptRoot "MingLu_Windows.zip"

New-Item -ItemType Directory -Path $installRoot -Force | Out-Null
Expand-Archive -LiteralPath $zip -DestinationPath $installRoot -Force

$exe = Join-Path $installRoot "MingLu.exe"
if (-not (Test-Path -LiteralPath $exe)) {
    throw "MingLu.exe was not installed."
}

$desktop = [Environment]::GetFolderPath("DesktopDirectory")
$shortcutName = ([char]0x660E).ToString() + ([char]0x8DEF).ToString() + ".lnk"
$shortcutPath = Join-Path $desktop $shortcutName
$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $exe
$shortcut.WorkingDirectory = $installRoot
$shortcut.IconLocation = $exe
$shortcut.Save()

Start-Process -FilePath $exe -WorkingDirectory $installRoot
'@
Set-Content -LiteralPath (Join-Path $stagingPath "install.ps1") -Value $installPs1 -Encoding UTF8

$sedPath = Join-Path $stagingPath "MingLu_Setup.sed"
$escapedSetupPath = $setupPath.Replace("\", "\\")
$escapedStagingPath = $stagingPath.Replace("\", "\\")
$sed = @"
[Version]
Class=IEXPRESS
SEDVersion=3

[Options]
PackagePurpose=InstallApp
ShowInstallProgramWindow=0
HideExtractAnimation=1
UseLongFileName=1
InsideCompressed=0
CAB_FixedSize=0
CAB_ResvCodeSigning=0
RebootMode=N
InstallPrompt=
DisplayLicense=
FinishMessage=
TargetName=$escapedSetupPath
FriendlyName=MingLu Installer
AppLaunched=install.cmd
PostInstallCmd=<None>
AdminQuietInstCmd=install.cmd
UserQuietInstCmd=install.cmd
SourceFiles=SourceFiles

[Strings]
FILE0="MingLu_Windows.zip"
FILE1="install.cmd"
FILE2="install.ps1"

[SourceFiles]
SourceFiles0=$escapedStagingPath

[SourceFiles0]
%FILE0%=
%FILE1%=
%FILE2%=
"@
Set-Content -LiteralPath $sedPath -Value $sed -Encoding ASCII

$iexpress = Join-Path $env:WINDIR "System32\iexpress.exe"
if (Test-Path -LiteralPath $iexpress) {
    $proc = Start-Process -FilePath $iexpress -ArgumentList @("/N", "/Q", $sedPath) -Wait -PassThru -WindowStyle Hidden
    if ($proc.ExitCode -ne 0) {
        throw "IExpress failed with exit code $($proc.ExitCode)."
    }
} else {
    Write-Warning "iexpress.exe was not found; zip package was still created."
}

if (Test-Path -LiteralPath $stagingPath) { Remove-Item -LiteralPath $stagingPath -Recurse -Force }

Write-Host "Windows package: $zipPath"
if (Test-Path -LiteralPath $setupPath) {
    Write-Host "Windows installer: $setupPath"
}
