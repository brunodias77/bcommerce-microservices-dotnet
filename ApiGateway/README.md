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

## 🔌 Integração com Microsserviços

### Client Service
- **Endpoint**: `http://client-service:8081`
- **Rotas**: `/api/client/*`
- **Autenticação**: JWT obrigatório (exceto registro/login)

### Configuração YARP
```json
{
  "ReverseProxy": {
    "Routes": {
      "client-service": {
        "ClusterId": "client-service-cluster",
        "Match": {
          "Path": "/api/client/{**catch-all}"
        },
        "AuthorizationPolicy": "authenticated"
      }
    },
    "Clusters": {
      "client-service-cluster": {
        "Destinations": {
          "client-service": {
            "Address": "http://client-service:8081"
          }
        }
      }
    }
  }
}
```

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

### Testar Roteamento
```bash
# Criar usuário (público)
curl -X POST http://localhost:5000/api/client/create-user \
  -H "Content-Type: application/json" \
  -d '{"username": "teste", "email": "teste@teste.com"}'

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
  ports:
    - "5000:5000"
  environment:
    - ASPNETCORE_ENVIRONMENT=Development
    - ASPNETCORE_URLS=http://+:5000
  depends_on:
    - keycloak
    - client-service
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
```

## 📝 Próximos Passos

1. **Implementar Rate Limiting** com Polly
2. **Adicionar Circuit Breaker** para resiliência
3. **Implementar Cache** com Redis
4. **Adicionar Métricas** com Prometheus
5. **Implementar Tracing** com OpenTelemetry

## 🤝 Contribuição

1. Fork o projeto
2. Crie uma branch para sua feature
3. Commit suas mudanças
4. Push para a branch
5. Abra um Pull Request

## 📄 Licença

Este projeto está sob a licença MIT. 