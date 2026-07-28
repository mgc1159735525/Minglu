@echo off
cd /d "%~dp0"
node Tools\import_game_tables.js
if errorlevel 1 (
  echo.
  echo 回写失败，请检查上方错误。
  pause
  exit /b 1
)
echo.
echo 回写完成：Assets\Resources\Data\MingLuGameConfig.json 与 Assets\Resources\MingLuStoryData.json
pause
