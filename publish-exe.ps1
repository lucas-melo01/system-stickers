# Script para publicar o projeto como executável Windows

$projectPath = "SistemaEtiquetas.UI"
$outputPath = ".\PublishOutput"
$configuration = "Release"
$runtime = "win-x64"

Write-Host "🚀 Iniciando publicação como Self-Contained Deployment..." -ForegroundColor Green
Write-Host "📁 Projeto: $projectPath"
Write-Host "📦 Saída: $outputPath"
Write-Host "⚙️  Configuração: $configuration"
Write-Host "🖥️  Runtime: $runtime`n"

# Limpar output anterior
if (Test-Path $outputPath) {
    Write-Host "🗑️  Removendo publicação anterior..."
    Remove-Item $outputPath -Recurse -Force
}

# Publicar
dotnet publish $projectPath `
    -c $configuration `
    -r $runtime `
    --self-contained `
    -o $outputPath `
    --no-restore

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✅ Publicação concluída com sucesso!" -ForegroundColor Green
    
    $exePath = Join-Path $outputPath "SistemaEtiquetas.UI.exe"
    
    if (Test-Path $exePath) {
        Write-Host "📍 Executável criado em: $exePath"
        $fileSize = (Get-Item $exePath).Length / 1MB
        Write-Host "📊 Tamanho do .exe: $([Math]::Round($fileSize, 2)) MB"
        Write-Host "`n✨ A cliente pode copiar a pasta PublishOutput e executar SistemaEtiquetas.UI.exe"
    }
} else {
    Write-Host "`n❌ Erro durante a publicação!" -ForegroundColor Red
    exit 1
}
