$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw '.NET SDK 10 não encontrado. Instale o SDK 10 e ferramentas de desktop .NET.'
}
$root = $PSScriptRoot
if ((Test-Path (Join-Path $root 'src')) -or (Test-Path (Join-Path $root 'tests'))) {
    throw 'Já existem pastas src ou tests. Cancelado para não sobrescrever dados.'
}
Push-Location $root
try {
    & dotnet new sln -n OpenPad
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao criar solution.' }
    & dotnet new wpf -n OpenPad.App -o src/OpenPad.App --framework net10.0
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao criar WPF.' }
    & dotnet new classlib -n OpenPad.Core -o src/OpenPad.Core --framework net10.0
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao criar core.' }
    & dotnet new classlib -n OpenPad.Input -o src/OpenPad.Input --framework net10.0
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao criar input.' }
    & dotnet new nunit -n OpenPad.Tests -o tests/OpenPad.Tests --framework net10.0
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao criar testes.' }
    & dotnet sln add src/OpenPad.App/OpenPad.App.csproj src/OpenPad.Core/OpenPad.Core.csproj src/OpenPad.Input/OpenPad.Input.csproj tests/OpenPad.Tests/OpenPad.Tests.csproj
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao adicionar projetos à solution.' }
    & dotnet add src/OpenPad.App/OpenPad.App.csproj reference src/OpenPad.Core/OpenPad.Core.csproj src/OpenPad.Input/OpenPad.Input.csproj
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao referenciar projetos na UI.' }
    & dotnet add src/OpenPad.Input/OpenPad.Input.csproj reference src/OpenPad.Core/OpenPad.Core.csproj
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao referenciar Core no Input.' }
    & dotnet add tests/OpenPad.Tests/OpenPad.Tests.csproj reference src/OpenPad.Core/OpenPad.Core.csproj src/OpenPad.Input/OpenPad.Input.csproj
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao referenciar projetos nos testes.' }
    & dotnet build
    if ($LASTEXITCODE -ne 0) { throw 'dotnet build falhou.' }
    & dotnet test
    if ($LASTEXITCODE -ne 0) { throw 'dotnet test falhou.' }
    Write-Host 'Bootstrap da solução concluído. Veja docs/AGENT_PROMPT_01.md' -ForegroundColor Green
} finally { Pop-Location }
