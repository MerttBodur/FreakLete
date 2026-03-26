@echo off
setlocal
REM Runs the real ProfilePage tests inside a WinUI3 host.
REM MAUI controls require a platform host — dotnet test does not provide one.
REM Results are written to %USERPROFILE%\page-test-results.txt

set "PROJECT=%~dp0FreakLete.Page.Tests.csproj"
set "RESULT=%USERPROFILE%\page-test-results.txt"
set "EXE=%~dp0bin\Debug\net10.0-windows10.0.19041.0\win-x64\FreakLete.Page.Tests.exe"

echo Building hosted page tests...
del /q "%RESULT%" 2>nul
dotnet build "%PROJECT%" -nologo -v minimal
if errorlevel 1 (
    echo BUILD FAILED
    if exist "%RESULT%" type "%RESULT%"
    exit /b 1
)

if not exist "%EXE%" (
    echo ERROR: Test host executable not found: %EXE%
    exit /b 1
)

echo Running hosted page tests...
start "" /wait "%EXE%"
if errorlevel 1 (
    echo RUN FAILED
)

echo.
if exist "%RESULT%" (
    type "%RESULT%"
    findstr /r /c:"^[0-9][0-9]* tests: [0-9][0-9]* passed, [0-9][0-9]* failed$" "%RESULT%" >nul
    if errorlevel 1 (
        echo ERROR: Missing final summary line in artifact.
        exit /b 1
    )

    findstr /c:"FAILED TESTS:" "%RESULT%" >nul
    if errorlevel 1 (
        echo ERROR: Missing FAILED TESTS line in artifact.
        exit /b 1
    )

    findstr /r /c:"^[0-9][0-9]* tests: [0-9][0-9]* passed, 0 failed$" "%RESULT%" >nul
    if errorlevel 1 (
        echo TESTS REPORTED FAILURES
        exit /b 1
    )
) else (
    echo ERROR: No results file produced. The app may have crashed before tests ran.
    exit /b 1
)

exit /b 0
