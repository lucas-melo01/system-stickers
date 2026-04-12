# 📋 SCRIPT DE TESTES - Sistema de Etiquetas (PowerShell)

$BASE_URL = "https://localhost:44307"
$ErrorActionPreference = "Continue"

function Test-Endpoint {
    param(
        [string]$Name,
        [string]$Method,
        [string]$Endpoint,
        [object]$Body = $null
    )
    
    Write-Host "`n=== $Name ===" -ForegroundColor Cyan
    
    try {
        $params = @{
            Uri             = "$BASE_URL$Endpoint"
            Method          = $Method
            ContentType     = "application/json"
            SkipCertificateCheck = $true
        }
        
        if ($Body) {
            $params['Body'] = $Body | ConvertTo-Json -Depth 10
        }
        
        $response = Invoke-WebRequest @params
        Write-Host "✅ Status: $($response.StatusCode)" -ForegroundColor Green
        Write-Host "Response:" 
        Write-Host $response.Content | ConvertFrom-Json | ConvertTo-Json -Depth 10
    }
    catch {
        Write-Host "❌ Erro: $($_.Exception.Message)" -ForegroundColor Red
        if ($_.Exception.Response) {
            Write-Host "Status: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
        }
    }
}

# TESTE 1: Health Check
Test-Endpoint -Name "TESTE 1: Health Check" -Method "GET" -Endpoint "/"

# TESTE 2: GET Configuração
Test-Endpoint -Name "TESTE 2: GET Configuração" -Method "GET" -Endpoint "/config"

# TESTE 3: POST Webhook Pedido (CRÍTICO)
$pedidoPayload = @{
    id = 123456789
    data_criacao = "2024-12-12T10:30:00Z"
    cliente = @{
        nome = "João da Silva"
        cpf = "123.456.789-00"
    }
    itens = @(
        @{
            nome = "Camiseta Branca M"
            sku = "SKU-001-M-WHITE"
            quantidade = 2
        },
        @{
            nome = "Calça Preta G"
            sku = "SKU-002-G-BLACK"
            quantidade = 1
        }
    )
}

Test-Endpoint -Name "TESTE 3: POST Webhook Pedido (CRÍTICO)" -Method "POST" -Endpoint "/webhook/pedido" -Body $pedidoPayload

# TESTE 4: GET Pedidos
Test-Endpoint -Name "TESTE 4: GET Pedidos" -Method "GET" -Endpoint "/pedidos"

# TESTE 5: POST Webhook Pedido Duplicado
Test-Endpoint -Name "TESTE 5: POST Webhook Pedido Duplicado (deve rejeitar)" -Method "POST" -Endpoint "/webhook/pedido" -Body $pedidoPayload

# TESTE 6: Reimpressão (se houver pedidos com itens)
Write-Host "`n=== TESTE 6: POST Reimprimir Item ===" -ForegroundColor Cyan
Write-Host "Nota: Execute este teste se houver PedidoItens com Id=1" -ForegroundColor Yellow
Write-Host "Comando: curl -X POST 'https://localhost:44307/reimprimir/1' -k" -ForegroundColor Yellow

# Resumo
Write-Host "`n" 
Write-Host "=== RESUMO DOS TESTES ===" -ForegroundColor Green
Write-Host "✓ TESTE 1: Deve retornar 200 com 'API Rodando 🚀'" -ForegroundColor Green
Write-Host "✓ TESTE 2: Deve retornar 200 com configuração (ou array vazio)" -ForegroundColor Green
Write-Host "✓ TESTE 3: Deve retornar 200 com 'Pedido salvo com sucesso' (CRÍTICO)" -ForegroundColor Green
Write-Host "✓ TESTE 4: Deve retornar 200 com lista de pedidos" -ForegroundColor Green
Write-Host "✓ TESTE 5: Deve retornar 200 com 'Pedido já processado'" -ForegroundColor Green
Write-Host ""
Write-Host "⚠️  Se TESTE 3 falhar com 'null value in column Id':" -ForegroundColor Yellow
Write-Host "   → A migration não foi aplicada corretamente" -ForegroundColor Yellow
Write-Host "   → Execute: dotnet ef database update --project SistemaEtiquetas.Infrastructure --startup-project SistemaEtiquetas.API" -ForegroundColor Yellow
