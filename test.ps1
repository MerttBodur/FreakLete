param(
	[Parameter(ValueFromRemainingArguments = $true)]
	[string[]]$DotnetArgs
)

$ErrorActionPreference = 'Stop'
$env:DOTNET_CLI_HOME = $PSScriptRoot

Write-Host 'Running blocking tests: Core + API...' -ForegroundColor Cyan

$coreTestProject = Join-Path $PSScriptRoot 'FreakLete.Core.Tests\FreakLete.Core.Tests.csproj'
$apiTestProject = Join-Path $PSScriptRoot 'FreakLete.Api.Tests\FreakLete.Api.Tests.csproj'

Write-Host '--- FreakLete.Core.Tests ---' -ForegroundColor Yellow
dotnet test $coreTestProject --no-restore @DotnetArgs
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host '--- FreakLete.Api.Tests ---' -ForegroundColor Yellow
dotnet test $apiTestProject --no-restore @DotnetArgs
exit $LASTEXITCODE
