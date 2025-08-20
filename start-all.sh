#!/bin/bash

# Script para iniciar todos os serviços
echo "🚀 Iniciando todos os serviços..."
echo "================================"

# Iniciar ClientService
echo "📦 Iniciando ClientService..."
cd ClientService
./start.sh
if [ $? -ne 0 ]; then
    echo "❌ Erro ao iniciar ClientService!"
    exit 1
fi
cd ..

# Aguardar um pouco
echo "⏳ Aguardando 5 segundos..."
sleep 5

# Iniciar ApiGateway
echo "🌐 Iniciando ApiGateway..."
cd ApiGateway
./start.sh
if [ $? -ne 0 ]; then
    echo "❌ Erro ao iniciar ApiGateway!"
    exit 1
fi
cd ..

echo "✅ Todos os serviços foram iniciados com sucesso!"
echo "📍 ClientService: http://localhost:5122"
echo "📍 ApiGateway: http://localhost:5000"
echo ""
echo "📊 Status dos containers:"
docker ps -f name=clientservice -f name=apigateway