#!/bin/bash

# Script para parar o ClientService container
echo "🛑 Parando ClientService..."

# Verificar se o container está rodando
if [ ! "$(docker ps -q -f name=clientservice)" ]; then
    echo "⚠️  ClientService não está rodando!"
    
    # Verificar se existe um container parado
    if [ "$(docker ps -aq -f name=clientservice)" ]; then
        echo "🗑️  Removendo container parado..."
        docker rm clientservice
        echo "✅ Container removido!"
    fi
    exit 0
fi

# Parar o container
echo "⏹️  Parando container..."
docker stop clientservice

if [ $? -eq 0 ]; then
    echo "✅ ClientService parado com sucesso!"
    
    # Perguntar se deseja remover o container
    read -p "🗑️  Deseja remover o container? (y/N): " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        docker rm clientservice
        echo "✅ Container removido!"
    fi
else
    echo "❌ Erro ao parar o container!"
    exit 1
fi