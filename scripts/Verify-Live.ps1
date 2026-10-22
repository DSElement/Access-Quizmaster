param([switch] $SqlOnly)

$ErrorActionPreference = 'Stop'
$repositoryPath = Split-Path -Parent $PSScriptRoot
$previousSqlSetting = $env:AI_LABS_LIVE_SQL
$previousAiSetting = $env:AI_LABS_LIVE_AI

Push-Location -LiteralPath $repositoryPath
try {
    & (Join-Path $PSScriptRoot 'Initialize.ps1')
    $env:AI_LABS_LIVE_SQL = '1'
    $env:AI_LABS_LIVE_AI = if ($SqlOnly) { '0' } else { '1' }
    $testArguments = @('test', 'tests/QuizMaster.Tests/QuizMaster.Tests.csproj', '--verbosity', 'minimal')
    if ($SqlOnly) { $testArguments += @('--filter', 'FullyQualifiedName~SqlServerTests') }
    & dotnet @testArguments
    if ($LASTEXITCODE -ne 0) { throw 'Backend or live-service verification failed. See the test output above.' }
    npm.cmd test --prefix frontend
    if ($LASTEXITCODE -ne 0) { throw 'Frontend verification failed. See the test output above.' }
    npm.cmd run build --prefix frontend
    if ($LASTEXITCODE -ne 0) { throw 'Frontend build failed. See the output above.' }
    if ($SqlOnly) {
        Write-Output 'SQL persistence checks, frontend tests, and frontend build passed.'
    }
    else {
        Write-Output 'All automated and live-service checks passed. Follow the README browser walkthrough to verify Microsoft sign-in and the complete UI.'
    }
}
finally {
    $env:AI_LABS_LIVE_SQL = $previousSqlSetting
    $env:AI_LABS_LIVE_AI = $previousAiSetting
    Pop-Location
}
