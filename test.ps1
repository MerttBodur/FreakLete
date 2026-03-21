param(
	[Parameter(ValueFromRemainingArguments = $true)]
	[string[]]$DotnetArgs
)

$ErrorActionPreference = 'Stop'
$env:DOTNET_CLI_HOME = $PSScriptRoot

$testProject = Join-Path $PSScriptRoot 'GymTracker.Core.Tests\GymTracker.Core.Tests.csproj'

dotnet test $testProject --no-restore @DotnetArgs
exit $LASTEXITCODE
