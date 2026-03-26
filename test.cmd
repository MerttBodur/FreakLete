@echo off
set DOTNET_CLI_HOME=%~dp0
echo Running blocking tests: Core + API...
echo.
echo --- FreakLete.Core.Tests ---
dotnet test "%~dp0FreakLete.Core.Tests\FreakLete.Core.Tests.csproj" --no-restore %*
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%
echo.
echo --- FreakLete.Api.Tests ---
dotnet test "%~dp0FreakLete.Api.Tests\FreakLete.Api.Tests.csproj" --no-restore %*
exit /b %ERRORLEVEL%
