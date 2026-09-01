@echo off
setlocal
pushd "%~dp0"

dotnet publish ".\EliteSaveEditor\EliteSaveEditor.csproj" -p:PublishProfile=win-x64 --nologo
if errorlevel 1 (
    popd
    exit /b 1
)

echo.
echo Standalone executable created at:
echo %~dp0publish\win-x64\EliteSaveEditor.exe

popd
