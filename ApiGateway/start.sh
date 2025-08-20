#!/bin/bash

# Script para iniciar o ApiGateway container
echo "🚀 Iniciando ApiGateway..."

# Verificar se o container já está rodando
if [ "$(docker ps -q -f name=apigateway)" ]; then
    echo "⚠️  ApiGateway já está rodando!"
    docker ps -f name=apigateway
    exit 0
fi

# Verificar se existe um container parado com o mesmo nome
if [ "$(docker ps -aq -f name=apigateway)" ]; then
    echo "🗑️  Removendo container anterior..."
    docker rm apigateway
fi

# Navegar para o diretório do ApiGateway
cd ApiGateway

# Build da imagem
echo "🔨 Construindo imagem do ApiGateway..."
docker build -t apigateway:latest .

if [ $? -ne 0 ]; then
    echo "❌ Erro ao construir a imagem!"
    exit 1
fi

# Executar o container
echo "▶️  Iniciando container..."
docker run -d \
    --name apigateway \
    --network host \
    -p 5000:5000 \
    -e ASPNETCORE_ENVIRONMENT=Development \
    -e ASPNETCORE_URLS=http://+:5000 \
    apigateway:latest

if [ $? -eq 0 ]; then
    echo "✅ ApiGateway iniciado com sucesso!"
    echo "📍 Disponível em: http://localhost:5000"
    echo "📊 Status do container:"
    docker ps -f name=apigateway
else
    echo "❌ Erro ao iniciar o container!"
    exit 1
fi