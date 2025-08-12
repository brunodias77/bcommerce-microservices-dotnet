#!/bin/bash

# B-Commerce Infrastructure Startup Script
echo "🚀 Iniciando infraestrutura B-Commerce..."

# Verificar se Docker está rodando
if ! docker info > /dev/null 2>&1; then
    echo "❌ Docker não está rodando. Por favor, inicie o Docker Desktop."
    exit 1
fi

# Criar diretórios necessários
echo "📁 Criando diretórios necessários..."
mkdir -p infra-service/rabbitmq
mkdir -p infra-service/keycloak
mkdir -p infra-service/scripts

# Verificar se arquivos de configuração existem
if [ ! -f "infra-service/rabbitmq/definitions.json" ]; then
    echo "⚠️  Arquivo definitions.json não encontrado. Criando..."
    # O arquivo será criado automaticamente
fi

# Iniciar serviços em ordem
echo "🐘 Iniciando PostgreSQL..."
docker-compose up -d postgres

echo "🔑 Iniciando Keycloak Database..."
docker-compose up -d keycloak-db

echo "⏳ Aguardando Keycloak Database ficar pronto..."
docker-compose exec keycloak-db pg_isready -U keycloak_user -d keycloak

echo "🔐 Iniciando Keycloak..."
docker-compose up -d keycloak

echo "🐰 Iniciando RabbitMQ..."
docker-compose up -d rabbitmq

echo "⏳ Aguardando todos os serviços ficarem prontos..."
sleep 30

# Verificar status dos serviços
echo "📊 Status dos serviços:"
docker-compose ps

echo "✅ Infraestrutura B-Commerce iniciada com sucesso!"
echo ""
echo "🌐 Serviços disponíveis:"
echo "   • PostgreSQL: localhost:5432"
echo "   • Keycloak: http://localhost:8080 (admin/admin123)"
echo "   • RabbitMQ Management: http://localhost:15672 (admin/admin123)"
echo ""
echo "📝 Para parar os serviços: ./infra-service/scripts/stop.sh"