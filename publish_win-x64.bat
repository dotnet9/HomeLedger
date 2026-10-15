@echo off
rem HomeLedger Windows x64 原生 AOT 发布
setlocal
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/publish.ps1 -RuntimeIdentifier win-x64 %*
if errorlevel 1 exit /b 1
echo.
echo 发布完成：publish\win-x64\HomeLedger.Desktop.exe
endlocal
