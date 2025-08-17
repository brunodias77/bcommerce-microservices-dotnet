# ClientService - Microserviço de Gestão de Clientes
## 📋 Sobre o Projeto
O ClientService é um microserviço desenvolvido em .NET 8 que gerencia o ciclo de vida completo dos clientes em um sistema de e-commerce. Implementa Clean Architecture e Domain-Driven Design (DDD) para garantir alta qualidade, manutenibilidade e escalabilidade.

## 🏗️ Arquitetura
### Clean Architecture
O projeto segue os princípios da Clean Architecture com separação clara de responsabilidades:

```
├── ClientService.Domain/          # 
Regras de negócio e entidades
├── ClientService.Application/     # 
Casos de uso e serviços
├── ClientService.Infrastructure/  # 
Implementações e integrações
└── ClientService.Api/            # 
Controllers e configurações
```
### Domain-Driven Design (DDD)
- Aggregates : Client como aggregate root
- Value Objects : Email, CPF, Phone com validações
- Domain Events : Para comunicação assíncrona
- Repositories : Abstração para persistência
- Domain Services : Lógicas de negócio complexas
## 🚀 Tecnologias Utilizadas
### Core
- .NET 8 - Framework principal
- C# 12 - Linguagem de programação
- ASP.NET Core - Web API framework
### Persistência
- Entity Framework Core 8.0.19 - ORM
- PostgreSQL - Banco de dados principal
- Npgsql - Driver PostgreSQL para .NET
### Messaging & Events
- MassTransit 8.5.2 - Framework de messaging
- RabbitMQ - Message broker
### Autenticação & Autorização
- Keycloak - Identity and Access Management
- JWT Bearer - Tokens de autenticação
- System.IdentityModel.Tokens.Jwt - Manipulação de JWT
### Documentação & Desenvolvimento
- Swagger/OpenAPI - Documentação da API
- DotNetEnv - Gerenciamento de variáveis de ambiente
## 📁 Estrutura do Projeto
```
ClientService/
├── src/
│   ├── ClientService.Api/              # 
Camada de apresentação
│   │   ├── Controllers/                # 
Controllers da API
│   │   ├── Configurations/             # 
Configurações DI
│   │   └── Program.cs                  # 
Entry point
│   ├── ClientService.Application/      # 
Camada de aplicação
│   │   ├── UseCases/                   # 
Casos de uso
│   │   ├── Services/                   # 
Interfaces de serviços
│   │   └── Common/                     # 
DTOs e utilitários
│   ├── ClientService.Domain/           # 
Camada de domínio
│   │   ├── Aggregates/                 # 
Aggregate roots
│   │   ├── Entities/                   # 
Entidades
│   │   ├── ValueObjects/               # 
Value objects
│   │   ├── Events/                     # 
Domain events
│   │   ├── Repositories/               # 
Interfaces de repositório
│   │   ├── Services/                   # 
Domain services
│   │   ├── Validations/                # 
Validações
│   │   └── Enums/                      # 
Enumerações
│   └── ClientService.Infra/            # 
Camada de infraestrutura
│       ├── Data/                       # 
Contexto do EF
│       ├── Repositories/               # 
Implementações de repositório
│       ├── Services/                   # 
Implementações de serviços
│       ├── ConfigEntityFramework/      # 
Configurações EF
│       ├── Migrations/                 # 
Migrações do banco
│       └── Events/                     # 
Handlers de eventos
└── tests/
    ├── ClientService.UnitTests/        # 
    Testes unitários
    └── ClientService.IntegrationTests/ # 
    Testes de integração
```
## 🔧 Configuração e Instalação
### Pré-requisitos
- .NET 8 SDK
- PostgreSQL 13+
- RabbitMQ 3.8+
- Keycloak (para autenticação)
- Docker (opcional)
### Configuração do Ambiente
1. 1.
   Clone o repositório
```
git clone <repository-url>
cd ClientService
```
2. 1.
   Configure as variáveis de ambiente Crie um arquivo .env na raiz do projeto:
```
# Database
DATABASE_CONNECTION_STRING=Host=localhost;
Database=clientservice;Username=postgres;
Password=yourpassword

# Keycloak
KEYCLOAK_BASE_URL=http://localhost:8080
KEYCLOAK_REALM=your-realm
KEYCLOAK_CLIENT_ID=your-client-id
KEYCLOAK_CLIENT_SECRET=your-client-secret

# RabbitMQ
RABBITMQ_CONNECTION_STRING=amqp://
guest:guest@localhost:5672/

# JWT
JWT_SECRET_KEY=your-super-secret-key
JWT_ISSUER=ClientService
JWT_AUDIENCE=ClientService
```
3. 1.
   Restaure as dependências
```
dotnet restore
```
4. 1.
   Execute as migrações do banco
```
dotnet ef database update --project src/
ClientService.Infra --startup-project src/
ClientService.Api
```
5. 1.
   Execute o projeto
```
dotnet run --project src/ClientService.Api
```
## 🔌 Endpoints da API
### Autenticação
- POST /api/client/create-user - Criar novo cliente
- POST /api/client/login - Login do cliente
### Gestão de Clientes
- GET /api/client/profile - Obter perfil do cliente (autenticado)
- PUT /api/client/profile - Atualizar perfil do cliente
- POST /api/client/address - Adicionar endereço
- PUT /api/client/address/{id} - Atualizar endereço
- DELETE /api/client/address/{id} - Remover endereço
### Documentação
- GET /swagger - Documentação interativa da API
## 🧪 Testes
### Executar Testes Unitários
```
dotnet test tests/ClientService.UnitTests/
```
### Executar Testes de Integração
```
dotnet test tests/ClientService.
IntegrationTests/
```
### Executar Todos os Testes
```
dotnet test
```
## 📊 Funcionalidades Principais
### Gestão de Clientes
- ✅ Cadastro de clientes com validações robustas
- ✅ Autenticação via Keycloak
- ✅ Gestão de perfis e informações pessoais
- ✅ Controle de status (Ativo, Inativo, Banido)
- ✅ Gestão de endereços múltiplos
- ✅ Controle de tentativas de login
- ✅ Verificação de email
### Recursos Técnicos
- ✅ Clean Architecture
- ✅ Domain-Driven Design
- ✅ CQRS Pattern
- ✅ Repository Pattern
- ✅ Unit of Work
- ✅ Domain Events
- ✅ Result Pattern para tratamento de erros
- ✅ Validation com Notification Pattern
- ✅ Logging estruturado
- ✅ Swagger/OpenAPI documentation
## 🔒 Segurança
- Autenticação JWT via Keycloak
- Autorização baseada em roles
- Validação de entrada em todos os endpoints
- Encriptação de senhas com hash seguro
- Proteção contra ataques de força bruta
- Validação de email obrigatória
## 📈 Monitoramento e Observabilidade
- Logging estruturado com Microsoft.Extensions.Logging
- Health checks para dependências externas
- Metrics customizadas para business events
- Correlation IDs para rastreamento de requests
## 🚀 Deploy
### Docker
```
# Build da imagem
docker build -t clientservice:latest .

# Executar container
docker run -p 5000:80 clientservice:latest
```
### Kubernetes
```
# Exemplo de deployment
apiVersion: apps/v1
kind: Deployment
metadata:
  name: clientservice
spec:
  replicas: 3
  selector:
    matchLabels:
      app: clientservice
  template:
    metadata:
      labels:
        app: clientservice
    spec:
      containers:
      - name: clientservice
        image: clientservice:latest
        ports:
        - containerPort: 80
```
## 🤝 Contribuição
1. 1.
   Fork o projeto
2. 2.
   Crie uma branch para sua feature ( git checkout -b feature/AmazingFeature )
3. 3.
   Commit suas mudanças ( git commit -m 'Add some AmazingFeature' )
4. 4.
   Push para a branch ( git push origin feature/AmazingFeature )
5. 5.
   Abra um Pull Request
## 📝 Convenções de Código
- C# Coding Standards seguindo as diretrizes da Microsoft
- Clean Code principles
- SOLID principles
- DRY (Don't Repeat Yourself)
- YAGNI (You Aren't Gonna Need It)
## 📚 Documentação Adicional
- Clean Architecture
- Domain-Driven Design
- .NET 8 Documentation
- Entity Framework Core
- MassTransit Documentation
## 📄 Licença
Este projeto está sob a licença MIT. Veja o arquivo LICENSE para mais detalhes.

## 👥 Equipe
- Desenvolvedor Principal : [Seu Nome]
- Arquiteto de Software : [Nome do Arquiteto]
- DevOps Engineer : [Nome do DevOps]

