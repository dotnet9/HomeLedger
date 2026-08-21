@echo off
rem HomeLedger Windows x64 单文件发布
setlocal
cd /d "%~dp0"
dotnet publish src/HomeLedger.Desktop/HomeLedger.Desktop.csproj -c Release -f net10.0 -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish/win-x64
echo.
echo 发布完成：publish\win-x64\HomeLedger.Desktop.exe
endlocal
