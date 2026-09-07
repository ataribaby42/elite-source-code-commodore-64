@echo off
setlocal
rem Build from this directory even when called from elsewhere.
pushd "%~dp0"
if errorlevel 1 exit /b 1

py -3 -c "import sys; sys.exit(sys.version_info < (3, 10))" >nul 2>&1
if not errorlevel 1 goto python_launcher
python -c "import sys; sys.exit(sys.version_info < (3, 10))" >nul 2>&1
if not errorlevel 1 goto python_path

rem Also support python.org installations that were not added to PATH.
set "PYTHON_EXE="
for /d %%D in ("%LOCALAPPDATA%\Programs\Python\Python3*") do if exist "%%~fD\python.exe" set "PYTHON_EXE=%%~fD\python.exe"
if not defined PYTHON_EXE goto no_python
"%PYTHON_EXE%" -c "import sys; sys.exit(sys.version_info < (3, 10))" >nul 2>&1
if errorlevel 1 goto no_python
"%PYTHON_EXE%" build.py %*
goto finish

:python_launcher
py -3 build.py %*
goto finish

:python_path
python build.py %*
goto finish

:no_python
echo ERROR: Python 3.10 or newer is required. Install Python or add it to PATH.
popd
exit /b 1

:finish
set "BUILD_RESULT=%ERRORLEVEL%"
popd
exit /b %BUILD_RESULT%
