#!/bin/bash

# Script para parar todos os serviços
echo "🛑 Parando todos os serviços..."
echo "=============================="

# Parar ApiGateway
echo "🌐 Parando ApiGateway..."
cd ApiGateway
./stop.sh
cd ..

# Parar ClientService
echo "📦 Parando ClientService..."
cd ClientService
./stop.sh
cd ..

echo "✅ Todos os serviços foram parados!"