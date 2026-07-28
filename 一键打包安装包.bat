@echo off
chcp 65001 >nul
setlocal

set "SCRIPT=%~dp0Tools\build_windows_installer.ps1"

echo Building MingLu Windows installer...
echo Project: %~dp0
echo.

powershell -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT%"
set "EXIT_CODE=%ERRORLEVEL%"

echo.
if "%EXIT_CODE%"=="0" (
  echo Done. Installer files are in Builds\Installers.
) else (
  echo Failed. Exit code: %EXIT_CODE%
)
echo.
pause
exit /b %EXIT_CODE%
