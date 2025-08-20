#!/bin/bash

# Script para iniciar o ClientService container
echo "🚀 Iniciando ClientService..."

# Verificar se o container já está rodando
if [ "$(docker ps -q -f name=clientservice)" ]; then
    echo "⚠️  ClientService já está rodando!"
    docker ps -f name=clientservice
    exit 0
fi

# Verificar se existe um container parado com o mesmo nome
if [ "$(docker ps -aq -f name=clientservice)" ]; then
    echo "🗑️  Removendo container anterior..."
    docker rm clientservice
fi

# Build da imagem
echo "🔨 Construindo imagem do ClientService..."
docker build -t clientservice:latest .

if [ $? -ne 0 ]; then
    echo "❌ Erro ao construir a imagem!"
    exit 1
fi

# Executar o container
echo "▶️  Iniciando container..."
docker run -d \
    --name clientservice \
    --network host \
    -p 5122:5122 \
    -e ASPNETCORE_ENVIRONMENT=Development \
    -e ASPNETCORE_URLS=http://+:5122 \
    clientservice:latest

if [ $? -eq 0 ]; then
    echo "✅ ClientService iniciado com sucesso!"
    echo "📍 Disponível em: http://localhost:5122"
    echo "📊 Status do container:"
    docker ps -f name=clientservice
else
    echo "❌ Erro ao iniciar o container!"
    exit 1
fi