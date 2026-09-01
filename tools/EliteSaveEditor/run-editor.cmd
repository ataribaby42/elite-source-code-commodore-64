@echo off
setlocal
pushd "%~dp0"
dotnet run --project ".\EliteSaveEditor\EliteSaveEditor.csproj" -- %*
set "editorExit=%ERRORLEVEL%"
popd
endlocal & exit /b %editorExit%
