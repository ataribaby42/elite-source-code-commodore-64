@echo off
setlocal
pushd "%~dp0"
if errorlevel 1 exit /b 1

call :build_all
set "ELITE_BUILD_RESULT=%errorlevel%"
popd
exit /b %ELITE_BUILD_RESULT%

:build_all
rem Remove only the six images rebuilt below; keep README files and other data.
for %%F in (
    "5-compiled-game-disks\elite-commodore-64-flicker-free-gma86-pal.d64"
    "5-compiled-game-disks\elite-commodore-64-flicker-free-gma85-ntsc.d64"
    "5-compiled-game-tapes\elite-commodore-64-flicker-free-pal.tap"
    "5-compiled-game-tapes\elite-commodore-64-flicker-free-ntsc.tap"
    "5-compiled-game-cartridges\elite-commodore-64-flicker-free-easyflash-pal.crt"
    "5-compiled-game-cartridges\elite-commodore-64-flicker-free-easyflash-ntsc.crt"
) do (
    if exist "%%~F\" (
        echo ERROR: Expected a file, but found a directory: %%~F
        exit /b 1
    )
    if exist "%%~F" (
        echo Removing old build output: %%~F
        del /q "%%~F"
        if exist "%%~F" (
            echo ERROR: Could not remove old build output: %%~F
            exit /b 1
        )
    )
)

call build_disk.bat
if errorlevel 1 exit /b %errorlevel%
call build_tape.bat
if errorlevel 1 exit /b %errorlevel%
call build_crt.bat
if errorlevel 1 exit /b %errorlevel%
exit /b 0
