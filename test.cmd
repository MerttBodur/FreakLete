@echo off
set DOTNET_CLI_HOME=%~dp0
dotnet test "%~dp0GymTracker.Core.Tests\GymTracker.Core.Tests.csproj" --no-restore %*
exit /b %ERRORLEVEL%
