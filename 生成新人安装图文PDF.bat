@echo off
setlocal
cd /d "%~dp0"
"C:\Users\Administrator\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe" Tools\build_newcomer_pdf.py
if errorlevel 1 (
  echo.
  echo Failed to build newcomer PDF.
  pause
  exit /b 1
)
echo.
echo PDF generated: Docs\新人安装与拉取项目教程_图文版.pdf
pause
