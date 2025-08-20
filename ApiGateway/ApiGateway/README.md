# B-Commerce API Gateway

## 📋 Sobre o Projeto

O API Gateway é o ponto de entrada centralizado para todos os microsserviços do B-Commerce. Implementado usando **YARP (Yet Another Reverse Proxy)** e **.NET 8**, ele fornece roteamento inteligente, autenticação JWT integrada com Keycloak, e funcionalidades de observabilidade.

## 🏗️ Arquitetura

### Tecnologias Utilizadas
- **.NET 8** - Framework principal
- **YARP** - Reverse proxy de alta performance
- **JWT Bearer** - Autenticação baseada em tokens
- **Keycloak** - Identity and Access Management
- **CORS** - Cross-Origin Resource Sharing

### Funcionalidades Principais
- ✅ **Roteamento Inteligente**: Proxy reverso para microsserviços
- ✅ **Autenticação JWT**: Validação de tokens via Keycloak
- ✅ **Autorização**: Controle de acesso baseado em políticas
- ✅ **CORS**: Suporte a origens específicas
- ✅ **Logging Estruturado**: Rastreamento de requisições com correlation IDs
- ✅ **Health Checks**: Monitoramento de saúde do gateway
- ✅ **Swagger/OpenAPI**: Documentação interativa da API

## 🔌 Integração com Microsserviços

### Client Service
O **Client Service** é responsável por todo o gerenciamento de usuários, incluindo:

#### Endpoints Públicos (Sem Autenticação)
- **POST** `/api/client/create-user` - Criação de novo usuário
- **POST** `/api/client/login` - Login de usuário

#### Endpoints Protegidos (Com Autenticação JWT)
- **GET** `/api/client/profile` - Obter perfil do usuário
- **PUT** `/api/client/profile` - Atualizar perfil
- **POST** `/api/client/address` - Adicionar endereço
- **PUT** `/api/client/address/{id}` - Atualizar endereço
- **DELETE** `/api/client/address/{id}` - Remover endereço

### Fluxo de Autenticação

1. **Registro**: Usuário acessa `/api/client/create-user` (público)
2. **Login**: Usuário acessa `/api/client/login` (público)
3. **Autenticação**: Client Service valida credenciais e retorna JWT
4. **Acesso Protegido**: Usuário usa JWT para acessar endpoints protegidos
5. **Validação**: API Gateway valida JWT antes de rotear para Client Service

## 🚀 Configuração

### Variáveis de Ambiente
```bash
# Keycloak
Keycloak__Authority=http://keycloak:8080/realms/b-commerce-realm
Keycloak__Audience=b-commerce-backend

# CORS
Cors__AllowedOrigins__0=http://localhost:4200
Cors__AllowedOrigins__1=http://localhost:3000
```

### Rotas Configuradas
| Rota | Serviço | Autenticação | Descrição |
|------|----------|---------------|-----------|
| `/api/client/*` | Client Service | ✅ Obrigatória | Todas as operações de cliente |
| `/api/client/create-user` | Client Service | ❌ Pública | Criação de usuário |
| `/api/client/login` | Client Service | ❌ Pública | Login de usuário |
| `/api/gateway/health` | Gateway | ❌ Pública | Health check |
| `/api/gateway/info` | Gateway | ❌ Pública | Informações do gateway |

## 🔧 Desenvolvimento

### Pré-requisitos
- .NET 8 SDK
- Docker e Docker Compose
- Keycloak rodando
- Client Service rodando

### Executar Localmente
```bash
cd ApiGateway/ApiGateway
dotnet restore
dotnet run
```

### Executar com Docker
```bash
# Build da imagem
docker build -t b-commerce-api-gateway .

# Executar container
docker run -p 5000:5000 b-commerce-api-gateway
```

## 🔒 Segurança

### Autenticação JWT
- Validação de issuer (Keycloak)
- Validação de audience
- Validação de lifetime
- Clock skew configurado

### Políticas de Autorização
- `authenticated`: Requer usuário autenticado
- Rotas públicas para registro e login
- Middleware de validação de tokens

## 📊 Observabilidade

### Logging
- Correlation IDs para rastreamento
- Métricas de performance (tempo de resposta)
- Logs estruturados com Serilog

### Health Checks
- Endpoint `/health` para monitoramento
- Integração com sistemas de monitoramento

### Métricas
- Tempo de resposta por rota
- Contagem de requisições
- Status codes de resposta

## 🧪 Testes

### Testar Gateway
```bash
# Health check
curl http://localhost:5000/api/gateway/health

# Informações do gateway
curl http://localhost:5000/api/gateway/info

# Validação de token
curl -X POST http://localhost:5000/api/gateway/validate-token \
  -H "Content-Type: application/json" \
  -d '{"token": "seu-jwt-token"}'
```

### Testar Roteamento para Client Service
```bash
# Criar usuário (público)
curl -X POST http://localhost:5000/api/client/create-user \
  -H "Content-Type: application/json" \
  -d '{
    "username": "teste",
    "email": "teste@teste.com",
    "firstName": "Usuário",
    "lastName": "Teste",
    "password": "senha123",
    "role": "USER"
  }'

# Login (público)
curl -X POST http://localhost:5000/api/client/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "teste",
    "password": "senha123"
  }'

# Perfil do cliente (autenticado)
curl -H "Authorization: Bearer seu-jwt-token" \
  http://localhost:5000/api/client/profile
```

## 🚀 Deploy

### Docker Compose
O gateway está configurado no `docker-compose.yml` principal:

```yaml
api-gateway:
  build:
    context: ./ApiGateway
    dockerfile: Dockerfile
  container_name: b-commerce-api-gateway
  ports:
    - "5000:5000"
  environment:
    - ASPNETCORE_ENVIRONMENT=Development
    - ASPNETCORE_URLS=http://+:5000
    - Keycloak__Authority=http://keycloak:8080/realms/b-commerce-realm
    - Keycloak__Audience=b-commerce-backend
  depends_on:
    - keycloak
    - client-service
  networks:
    - b-commerce-network
  healthcheck:
    test: ["CMD-SHELL", "curl -f http://localhost:5000/api/gateway/health || exit 1"]
    interval: 30s
    timeout: 10s
    retries: 3
    start_period: 40s
  restart: unless-stopped
```

### Kubernetes
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: api-gateway
spec:
  replicas: 3
  selector:
    matchLabels:
      app: api-gateway
  template:
    metadata:
      labels:
        app: api-gateway
    spec:
      containers:
      - name: api-gateway
        image: b-commerce-api-gateway:latest
        ports:
        - containerPort: 5000
        env:
        - name: Keycloak__Authority
          value: "http://keycloak:8080/realms/b-commerce-realm"
        - name: Keycloak__Audience
          value: "b-commerce-backend"
```

## 📝 Próximos Passos

1. **Implementar Rate Limiting** com Polly
2. **Adicionar Circuit Breaker** para resiliência
3. **Implementar Cache** com Redis
4. **Adicionar Métricas** com Prometheus
5. **Implementar Tracing** com OpenTelemetry

## 🔍 Troubleshooting

### Problemas Comuns

#### 1. Erro de CORS
```bash
# Verificar configuração CORS
curl -H "Origin: http://localhost:4200" \
  -H "Access-Control-Request-Method: POST" \
  -H "Access-Control-Request-Headers: Content-Type" \
  -X OPTIONS http://localhost:5000/api/client/create-user
```

#### 2. Erro de Autenticação
```bash
# Verificar se Keycloak está rodando
curl http://localhost:8080/health/ready

# Verificar configuração do realm
curl http://localhost:8080/realms/b-commerce-realm
```

#### 3. Erro de Roteamento
```bash
# Verificar se Client Service está rodando
curl http://localhost:8081/health

# Verificar logs do gateway
docker logs b-commerce-api-gateway
```

## 🤝 Contribuição

1. Fork o projeto
2. Crie uma branch para sua feature
3. Commit suas mudanças
4. Push para a branch
5. Abra um Pull Request

## 📄 Licença

Este projeto está sob a licença MIT. 