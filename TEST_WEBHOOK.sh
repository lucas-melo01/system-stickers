#!/bin/bash
# 📋 SCRIPT DE TESTES - Sistema de Etiquetas

# Cores para output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
RED='\033[0;31m'
NC='\033[0m' # No Color

BASE_URL="https://localhost:44307"

echo -e "${BLUE}=== TESTE 1: Health Check ===${NC}"
curl -k -X GET "${BASE_URL}/" \
  -w "\nStatus: %{http_code}\n\n"

echo -e "${BLUE}=== TESTE 2: GET Configuração ===${NC}"
curl -k -X GET "${BASE_URL}/config" \
  -H "Content-Type: application/json" \
  -w "\nStatus: %{http_code}\n\n"

echo -e "${BLUE}=== TESTE 3: POST Webhook Pedido (TESTE CRÍTICO) ===${NC}"
curl -k -X POST "${BASE_URL}/webhook/pedido" \
  -H "Content-Type: application/json" \
  -d '{
    "id": 123456789,
    "data_criacao": "2024-12-12T10:30:00Z",
    "cliente": {
      "nome": "João da Silva",
      "cpf": "123.456.789-00"
    },
    "itens": [
      {
        "nome": "Camiseta Branca M",
        "sku": "SKU-001-M-WHITE",
        "quantidade": 2
      },
      {
        "nome": "Calça Preta G",
        "sku": "SKU-002-G-BLACK",
        "quantidade": 1
      }
    ]
  }' \
  -w "\nStatus: %{http_code}\n\n"

echo -e "${BLUE}=== TESTE 4: GET Pedidos ===${NC}"
curl -k -X GET "${BASE_URL}/pedidos" \
  -H "Content-Type: application/json" \
  -w "\nStatus: %{http_code}\n\n"

echo -e "${BLUE}=== TESTE 5: POST Webhook Pedido Duplicado (deve rejeitar) ===${NC}"
curl -k -X POST "${BASE_URL}/webhook/pedido" \
  -H "Content-Type: application/json" \
  -d '{
    "id": 123456789,
    "data_criacao": "2024-12-12T10:30:00Z",
    "cliente": {
      "nome": "João da Silva",
      "cpf": "123.456.789-00"
    },
    "itens": [
      {
        "nome": "Camiseta Branca M",
        "sku": "SKU-001-M-WHITE",
        "quantidade": 2
      }
    ]
  }' \
  -w "\nStatus: %{http_code}\n\n"

echo -e "${GREEN}=== TESTES CONCLUÍDOS ===${NC}"
echo -e "${BLUE}Observações:${NC}"
echo "✓ TESTE 1: Deve retornar 200 com 'API Rodando 🚀'"
echo "✓ TESTE 2: Deve retornar 200 com configuração (ou array vazio)"
echo "✓ TESTE 3: Deve retornar 200 com 'Pedido salvo com sucesso' (CRÍTICO)"
echo "✓ TESTE 4: Deve retornar 200 com lista de pedidos"
echo "✓ TESTE 5: Deve retornar 200 com 'Pedido já processado'"
echo ""
echo "Se TESTE 3 falhar com 'null value in column Id', a migration não foi aplicada corretamente."
