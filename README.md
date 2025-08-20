# B-Commerce Microservices (.NET Edition)

## 📋 Sobre o Projeto

O **B-Commerce** é uma plataforma de e-commerce moderna, escalável e resiliente, desenvolvida em **.NET 8** com arquitetura de microsserviços desacoplada. O projeto implementa Clean Architecture, Domain-Driven Design (DDD) e utiliza tecnologias modernas como YARP, Keycloak e RabbitMQ.

## 🏗️ Arquitetura

### Microsserviços Implementados

| Serviço | Status | Porta | Descrição |
|----------|--------|-------|-----------|
| **Infra Service** | ✅ Completo | - | Infraestrutura base (PostgreSQL, Keycloak, RabbitMQ) |
| **API Gateway** | ✅ Completo | 5000 | Gateway centralizado com YARP e autenticação JWT |
| **Client Service** | ✅ Completo | 8081 | Gestão de clientes e autenticação |
| **Catalog Service** | ❌ Pendente | - | Catálogo de produtos e categorias |
| **Cart Service** | ❌ Pendente | - | Gestão de carrinho de compras |
| **Sales Service** | ❌ Pendente | - | Processamento de vendas e pedidos |

### Tecnologias Utilizadas

- **Backend**: .NET 8, ASP.NET Core, Entity Framework Core
- **API Gateway**: YARP (Yet Another Reverse Proxy)
- **Autenticação**: Keycloak (OIDC), JWT Bearer
- **Banco de Dados**: PostgreSQL
- **Mensageria**: RabbitMQ, MassTransit
- **Containerização**: Docker, Docker Compose

## 🚀 Início Rápido

### Pré-requisitos

- .NET 8 SDK
- Docker e Docker Compose
- Git

### 1. Clonar o Repositório

```bash
git clone <repository-url>
cd bcommerce-microservices-dotnet
```

### 2. Iniciar Infraestrutura

```bash
# Iniciar todos os serviços de infraestrutura
docker-compose up -d postgres keycloak rabbitmq

# Aguardar os serviços estarem prontos
docker-compose ps
```

### 3. Executar Microsserviços

```bash
# Build e execução do Client Service
cd ClientService
dotnet build
dotnet run --project src/ClientService.Api

# Build e execução do API Gateway
cd ../ApiGateway
dotnet build
dotnet run --project ApiGateway/ApiGateway
```

### 4. Acessar Serviços

- **API Gateway**: http://localhost:5000
- **Client Service**: http://localhost:8081
- **Keycloak**: http://localhost:8080 (admin/admin123)
- **RabbitMQ**: http://localhost:15672 (admin/admin123)
- **PostgreSQL**: localhost:5432

## 🔌 API Gateway

O **API Gateway** é o ponto de entrada centralizado para todos os microsserviços, implementado com **YARP** e as seguintes funcionalidades:

### Funcionalidades

- ✅ **Roteamento Inteligente**: Proxy reverso para microsserviços
- ✅ **Autenticação JWT**: Validação de tokens via Keycloak
- ✅ **Autorização**: Controle de acesso baseado em políticas
- ✅ **CORS**: Suporte a origens específicas
- ✅ **Logging Estruturado**: Rastreamento com correlation IDs
- ✅ **Health Checks**: Monitoramento de saúde

### Rotas Configuradas

| Rota | Serviço | Autenticação | Descrição |
|------|----------|---------------|-----------|
| `/api/client/*` | Client Service | ✅ Obrigatória | Todas as operações de cliente |
| `/api/client/create-user` | Client Service | ❌ Pública | Criação de usuário |
| `/api/client/login` | Client Service | ❌ Pública | Login de usuário |
| `/api/gateway/health` | Gateway | ❌ Pública | Health check |
| `/api/gateway/info` | Gateway | ❌ Pública | Informações do gateway |

### Testar Gateway

```bash
# Health check
curl http://localhost:5000/api/gateway/health

# Informações do gateway
curl http://localhost:5000/api/gateway/info

# Criar usuário (público)
curl -X POST http://localhost:5000/api/client/create-user \
  -H "Content-Type: application/json" \
  -d '{"username": "teste", "email": "teste@teste.com"}'
```

## 🔒 Autenticação e Segurança

### Keycloak

- **Realm**: `b-commerce-realm`
- **Clients**: `b-commerce-frontend` (público), `b-commerce-backend` (confidencial)
- **Roles**: `USER`, `ADMIN`
- **Usuário Admin**: admin/admin123

### JWT

- Validação de issuer (Keycloak)
- Validação de audience
- Validação de lifetime
- Clock skew configurado

## 📊 Observabilidade

### Health Checks

- **API Gateway**: `/api/gateway/health`
- **Client Service**: `/health`
- **Keycloak**: `/health/ready`
- **RabbitMQ**: Health check automático

### Logging

- Logs estruturados com correlation IDs
- Métricas de performance
- Rastreamento de requisições

## 🧪 Testes

### Executar Testes

```bash
# Testes unitários do Client Service
cd ClientService
dotnet test tests/ClientService.UnitTests/

# Testes de integração
dotnet test tests/ClientService.IntegrationTests/
```

## 🚀 Deploy

### Docker Compose

```bash
# Build e execução completa
docker-compose up --build

# Executar em background
docker-compose up -d

# Parar serviços
docker-compose down
```

### Desenvolvimento Local

```bash
# Executar apenas infraestrutura
docker-compose up -d postgres keycloak rabbitmq

# Executar microsserviços localmente
dotnet run --project ClientService/src/ClientService.Api
dotnet run --project ApiGateway/ApiGateway/ApiGateway
```

## 📁 Estrutura do Projeto

```
bcommerce-microservices-dotnet/
├── infra-service/           # Infraestrutura base
│   ├── data/               # Scripts SQL
│   ├── keycloak/           # Configuração Keycloak
│   ├── rabbitmq/           # Configuração RabbitMQ
│   └── specs/              # Especificações
├── ClientService/           # Microsserviço de clientes
│   ├── src/                # Código fonte
│   └── tests/              # Testes
├── ApiGateway/              # API Gateway com YARP
│   └── ApiGateway/         # Código fonte
├── docker-compose.yml       # Orquestração Docker
└── README.md               # Este arquivo
```

## 🔭 Próximos Passos

1. **Implementar Catalog Service** com CRUD de produtos
2. **Desenvolver Cart Service** para gestão de carrinho
3. **Criar Sales Service** para processamento de vendas
4. **Implementar Frontend Angular** com autenticação OIDC
5. **Adicionar Observabilidade** com OpenTelemetry e Prometheus

## 🤝 Contribuição

1. Fork o projeto
2. Crie uma branch para sua feature
3. Commit suas mudanças
4. Push para a branch
5. Abra um Pull Request

## 📄 Licença

Este projeto está sob a licença MIT.

## 👥 Equipe

- **Desenvolvedor Principal**: [Seu Nome]
- **Arquiteto de Software**: [Nome do Arquiteto]
- **DevOps Engineer**: [Nome do DevOps] 