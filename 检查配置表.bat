@echo off
cd /d "%~dp0"
node Tools\validate_game_tables.js
if errorlevel 1 (
  echo.
  echo Validation failed. Check the errors above.
  pause
  exit /b 1
)
echo.
echo Validation finished. Warnings can be cleaned up later.
pause
