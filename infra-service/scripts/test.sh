#!/bin/bash

# B-Commerce Infrastructure Integration Tests
echo "🧪 Executando testes de integração da infraestrutura..."

# Cores para output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Função para testar conectividade
test_service() {
    local service_name=$1
    local test_command=$2
    local expected_result=$3
    
    echo -n "🔍 Testando $service_name... "
    
    if eval $test_command > /dev/null 2>&1; then
        echo -e "${GREEN}✅ OK${NC}"
        return 0
    else
        echo -e "${RED}❌ FALHOU${NC}"
        return 1
    fi
}

# Contador de testes
total_tests=0
passed_tests=0

echo "📊 Iniciando testes..."
echo ""

# Teste 1: PostgreSQL
total_tests=$((total_tests + 1))
if test_service "PostgreSQL" "docker-compose exec -T postgres pg_isready -U bcommerce_user -d bcommerce" "ready"; then
    passed_tests=$((passed_tests + 1))
fi

# Teste 2: Keycloak Database
total_tests=$((total_tests + 1))
if test_service "Keycloak Database" "docker-compose exec -T keycloak-db pg_isready -U keycloak_user -d keycloak" "ready"; then
    passed_tests=$((passed_tests + 1))
fi

# Teste 3: Keycloak Health
total_tests=$((total_tests + 1))
if test_service "Keycloak Health" "curl -f http://localhost:8080/health/ready" "200"; then
    passed_tests=$((passed_tests + 1))
fi

# Teste 4: Keycloak Admin Console
total_tests=$((total_tests + 1))
if test_service "Keycloak Admin Console" "curl -f http://localhost:8080/admin/" "200"; then
    passed_tests=$((passed_tests + 1))
fi

# Teste 5: RabbitMQ Health
total_tests=$((total_tests + 1))
if test_service "RabbitMQ Health" "docker-compose exec -T rabbitmq rabbitmq-diagnostics -q ping" "pong"; then
    passed_tests=$((passed_tests + 1))
fi

# Teste 6: RabbitMQ Management
total_tests=$((total_tests + 1))
if test_service "RabbitMQ Management" "curl -f http://localhost:15672" "200"; then
    passed_tests=$((passed_tests + 1))
fi

# Teste 7: RabbitMQ API
total_tests=$((total_tests + 1))
if test_service "RabbitMQ API" "curl -f -u admin:admin123 http://localhost:15672/api/overview" "200"; then
    passed_tests=$((passed_tests + 1))
fi

echo ""
echo "📈 Resultados dos testes:"
echo "   Total: $total_tests"
echo -e "   Passou: ${GREEN}$passed_tests${NC}"
echo -e "   Falhou: ${RED}$((total_tests - passed_tests))${NC}"

if [ $passed_tests -eq $total_tests ]; then
    echo -e "\n${GREEN}🎉 Todos os testes passaram! Infraestrutura está funcionando corretamente.${NC}"
    exit 0
else
    echo -e "\n${RED}❌ Alguns testes falharam. Verifique os logs dos serviços.${NC}"
    echo "\n📋 Para verificar logs:"
    echo "   docker-compose logs postgres"
    echo "   docker-compose logs keycloak"
    echo "   docker-compose logs rabbitmq"
    exit 1
fi