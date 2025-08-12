#!/bin/bash

# B-Commerce Infrastructure Stop Script
echo "🛑 Parando infraestrutura B-Commerce..."

# Parar todos os serviços
docker-compose down

echo "✅ Infraestrutura B-Commerce parada com sucesso!"