@echo off
set DOTNET_CLI_HOME=%~dp0
dotnet test "%~dp0FreakLete.Core.Tests\FreakLete.Core.Tests.csproj" --no-restore %*
exit /b %ERRORLEVEL%
