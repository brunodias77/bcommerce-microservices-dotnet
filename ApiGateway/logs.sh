#!/bin/bash

# Script para visualizar logs do ApiGateway
echo "📋 Logs do ApiGateway:"
echo "====================="

# Verificar se o container existe
if [ ! "$(docker ps -aq -f name=apigateway)" ]; then
    echo "❌ Container ApiGateway não encontrado!"
    exit 1
fi

# Mostrar logs
docker logs -f apigateway