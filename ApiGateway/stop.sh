#!/bin/bash

# Script para parar o ApiGateway container
echo "🛑 Parando ApiGateway..."

# Verificar se o container está rodando
if [ ! "$(docker ps -q -f name=apigateway)" ]; then
    echo "⚠️  ApiGateway não está rodando!"
    
    # Verificar se existe um container parado
    if [ "$(docker ps -aq -f name=apigateway)" ]; then
        echo "🗑️  Removendo container parado..."
        docker rm apigateway
        echo "✅ Container removido!"
    fi
    exit 0
fi

# Parar o container
echo "⏹️  Parando container..."
docker stop apigateway

if [ $? -eq 0 ]; then
    echo "✅ ApiGateway parado com sucesso!"
    
    # Perguntar se deseja remover o container
    read -p "🗑️  Deseja remover o container? (y/N): " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        docker rm apigateway
        echo "✅ Container removido!"
    fi
else
    echo "❌ Erro ao parar o container!"
    exit 1
fi