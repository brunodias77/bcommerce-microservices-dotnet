#!/bin/bash

# B-Commerce Infrastructure Clean Script
echo "🧹 Limpando infraestrutura B-Commerce..."

read -p "⚠️  Isso irá remover TODOS os dados. Continuar? (y/N): " -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    echo "🛑 Parando serviços..."
    docker-compose down
    
    echo "🗑️  Removendo volumes..."
    docker-compose down -v
    
    echo "🧹 Removendo imagens não utilizadas..."
    docker system prune -f
    
    echo "✅ Limpeza concluída!"
else
    echo "❌ Operação cancelada."
fi