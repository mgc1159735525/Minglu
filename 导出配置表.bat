@echo off
cd /d "%~dp0"
node Tools\export_game_tables.js
if errorlevel 1 (
  echo.
  echo 导出失败，请检查上方错误。
  pause
  exit /b 1
)
echo.
echo 导出完成：DataTables\csv
pause
