$ErrorActionPreference = 'Stop'
$repositoryPath = Split-Path -Parent $PSScriptRoot

function Invoke-Checked {
    param([string] $Command, [string[]] $Arguments)
    & $Command @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$Command failed with exit code $LASTEXITCODE. Resolve the error above before continuing."
    }
}

Push-Location -LiteralPath $repositoryPath
try {
    if (-not (Test-Path -LiteralPath 'frontend/.env')) {
        Copy-Item -LiteralPath 'frontend/.env.example' -Destination 'frontend/.env'
    }
    Invoke-Checked 'SqlLocalDB.exe' @('start', 'MSSQLLocalDB')
    Invoke-Checked 'docker' @('compose', 'up', '-d', '--wait', 'qdrant', 'ollama')
    Invoke-Checked 'docker' @('compose', 'exec', 'ollama', 'ollama', 'pull', 'llama3.2:3b')
    Invoke-Checked 'docker' @('compose', 'exec', 'ollama', 'ollama', 'pull', 'embeddinggemma')
    $quizModelDefinition = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'quiz-model.json') -Raw
    $quizModelSettings = $quizModelDefinition | ConvertFrom-Json
    Invoke-Checked 'docker' @('compose', 'exec', 'ollama', 'ollama', 'pull', $quizModelSettings.from)
    $modelResult = Invoke-RestMethod -Uri 'http://localhost:11434/api/create' -Method Post `
        -ContentType 'application/json' -Body $quizModelDefinition -TimeoutSec 60
    if ($modelResult.status -ne 'success') { throw 'Ollama did not create the configured quiz model.' }
    Invoke-Checked 'dotnet' @('tool', 'restore')
    Invoke-Checked 'dotnet' @('restore', 'backend/QuizMaster.Api.csproj')
    Invoke-Checked 'dotnet' @('build', 'backend/QuizMaster.Api.csproj', '--no-restore', '--verbosity', 'minimal')
    Invoke-Checked 'dotnet' @('ef', 'database', 'update', '--project', 'backend', '--startup-project', 'backend', '--no-build')
    Invoke-Checked 'npm.cmd' @('ci', '--prefix', 'frontend')
    Write-Output 'Setup complete. Start the API and frontend in separate terminals using the README commands.'
}
finally {
    Pop-Location
}
