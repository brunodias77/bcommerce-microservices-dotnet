#!/bin/bash

# Script para visualizar logs do ClientService
echo "📋 Logs do ClientService:"
echo "========================"

# Verificar se o container existe
if [ ! "$(docker ps -aq -f name=clientservice)" ]; then
    echo "❌ Container ClientService não encontrado!"
    exit 1
fi

# Mostrar logs
docker logs -f clientservice