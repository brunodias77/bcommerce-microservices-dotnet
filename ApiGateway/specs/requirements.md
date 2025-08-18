# Requirements Document - API Gateway

## Introduction

O API Gateway é o ponto de entrada único para todas as requisições do B-Commerce, responsável por roteamento, autenticação, autorização, rate limiting, logging e resiliência. Este serviço utiliza YARP (Yet Another Reverse Proxy) com ASP.NET Core e implementa padrões de circuit breaker e retry com Polly.

## Requirements

### Requirement 1

**User Story:** Como um cliente ou sistema externo, eu quero acessar todos os serviços através de um único ponto de entrada, para que eu tenha uma interface unificada e consistente.

#### Acceptance Criteria

1. WHEN eu fizer requisição para `/api/clients/**` THEN ela SHALL ser roteada para Client Service
2. WHEN eu fizer requisição para `/api/catalog/**` THEN ela SHALL ser roteada para Catalog Service
3. WHEN eu fizer requisição para `/api/cart/**` THEN ela SHALL ser roteada para Cart Service
4. WHEN eu fizer requisição para `/api/sales/**` THEN ela SHALL ser roteada para Sales Service
5. WHEN serviço de destino estiver indisponível THEN gateway SHALL retornar erro apropriado
6. WHEN rota não existir THEN gateway SHALL retornar erro 404

### Requirement 2

**User Story:** Como um usuário, eu quero que minhas requisições sejam autenticadas automaticamente, para que eu possa acessar recursos protegidos de forma segura.

#### Acceptance Criteria

1. WHEN eu enviar token JWT válido THEN gateway SHALL validar token com Keycloak
2. WHEN token for inválido ou expirado THEN gateway SHALL retornar erro 401
3. WHEN eu acessar rota pública THEN autenticação SHALL ser opcional
4. WHEN eu acessar rota protegida sem token THEN gateway SHALL retornar erro 401
5. WHEN token for válido THEN claims SHALL ser repassados para serviços downstream
6. WHEN validação falhar THEN requisição SHALL ser rejeitada antes de chegar aos serviços

### Requirement 3

**User Story:** Como um administrador, eu quero que o gateway implemente autorização baseada em roles, para que apenas usuários com permissões adequadas acessem recursos específicos.

#### Acceptance Criteria

1. WHEN eu acessar endpoint admin THEN gateway SHALL verificar se tenho role "admin"
2. WHEN eu não tiver role necessária THEN gateway SHALL retornar erro 403
3. WHEN eu tiver múltiplas roles THEN gateway SHALL verificar se alguma é suficiente
4. WHEN role for verificada THEN informação SHALL ser repassada para serviço downstream
5. WHEN configuração de role mudar THEN gateway SHALL aplicar mudança sem restart
6. WHEN verificação falhar THEN requisição SHALL ser rejeitada com log detalhado

### Requirement 4

**User Story:** Como um administrador de sistema, eu quero implementar rate limiting para proteger os serviços de sobrecarga, para garantir disponibilidade e performance.

#### Acceptance Criteria

1. WHEN usuário exceder limite de requisições THEN gateway SHALL retornar erro 429
2. WHEN limite for atingido THEN gateway SHALL incluir headers com informações de rate limit
3. WHEN usuário for autenticado THEN limite SHALL ser maior que para usuários anônimos
4. WHEN endpoint for crítico THEN ele SHALL ter limite mais restritivo
5. WHEN janela de tempo resetar THEN contador SHALL voltar ao zero
6. WHEN rate limit for configurado THEN mudanças SHALL ser aplicadas dinamicamente

### Requirement 5

**User Story:** Como um desenvolvedor, eu quero que o gateway implemente circuit breaker e retry, para que o sistema seja resiliente a falhas temporárias.

#### Acceptance Criteria

1. WHEN serviço downstream falhar repetidamente THEN circuit breaker SHALL abrir
2. WHEN circuit breaker estiver aberto THEN requisições SHALL falhar rapidamente
3. WHEN circuit breaker estiver meio-aberto THEN algumas requisições SHALL ser testadas
4. WHEN serviço se recuperar THEN circuit breaker SHALL fechar automaticamente
5. WHEN requisição falhar temporariamente THEN gateway SHALL tentar novamente
6. WHEN retry esgotar tentativas THEN erro original SHALL ser retornado

### Requirement 6

**User Story:** Como um administrador, eu quero logs detalhados de todas as requisições, para que eu possa monitorar e debugar problemas.

#### Acceptance Criteria

1. WHEN requisição chegar THEN gateway SHALL logar método, URL, headers e timestamp
2. WHEN requisição for processada THEN gateway SHALL logar tempo de resposta e status
3. WHEN erro ocorrer THEN gateway SHALL logar detalhes do erro e stack trace
4. WHEN usuário for identificado THEN gateway SHALL logar ID do usuário
5. WHEN logs forem gerados THEN eles SHALL estar em formato estruturado (JSON)
6. WHEN volume de logs for alto THEN sistema SHALL implementar sampling

### Requirement 7

**User Story:** Como um desenvolvedor frontend, eu quero que o gateway configure CORS adequadamente, para que minha aplicação web possa fazer requisições cross-origin.

#### Acceptance Criteria

1. WHEN frontend fizer requisição THEN gateway SHALL incluir headers CORS apropriados
2. WHEN origem for permitida THEN requisição SHALL ser processada normalmente
3. WHEN origem não for permitida THEN gateway SHALL rejeitar requisição
4. WHEN requisição for preflight THEN gateway SHALL responder adequadamente
5. WHEN headers customizados forem enviados THEN eles SHALL ser permitidos se configurados
6. WHEN ambiente for desenvolvimento THEN CORS SHALL ser mais permissivo

### Requirement 8

**User Story:** Como um administrador, eu quero configurar o gateway dinamicamente, para que eu possa fazer ajustes sem reiniciar o serviço.

#### Acceptance Criteria

1. WHEN configuração mudar THEN gateway SHALL recarregar rotas automaticamente
2. WHEN novo serviço for adicionado THEN rota SHALL ser criada dinamicamente
3. WHEN serviço for removido THEN rota SHALL ser desabilitada automaticamente
4. WHEN configuração for inválida THEN gateway SHALL manter configuração anterior
5. WHEN mudança for aplicada THEN logs SHALL registrar a alteração
6. WHEN configuração for recarregada THEN conexões existentes SHALL ser mantidas

### Requirement 9

**User Story:** Como um desenvolvedor, eu quero que o gateway transforme requisições e respostas quando necessário, para manter compatibilidade entre versões de API.

#### Acceptance Criteria

1. WHEN requisição precisar de transformação THEN gateway SHALL modificar headers ou body
2. WHEN resposta precisar de transformação THEN gateway SHALL modificar antes de retornar
3. WHEN versão de API mudar THEN gateway SHALL manter compatibilidade com versões antigas
4. WHEN transformação falhar THEN gateway SHALL retornar erro apropriado
5. WHEN múltiplas transformações forem necessárias THEN elas SHALL ser aplicadas em ordem
6. WHEN transformação for configurada THEN ela SHALL ser aplicada automaticamente

### Requirement 10

**User Story:** Como um administrador, eu quero métricas e health checks do gateway, para que eu possa monitorar sua saúde e performance.

#### Acceptance Criteria

1. WHEN eu consultar health check THEN gateway SHALL verificar conectividade com serviços downstream
2. WHEN serviço downstream estiver indisponível THEN health check SHALL reportar degraded
3. WHEN eu consultar métricas THEN eu SHALL ver número de requisições, latência e erros
4. WHEN métricas forem coletadas THEN elas SHALL estar em formato Prometheus
5. WHEN gateway estiver sobrecarregado THEN métricas SHALL indicar problema
6. WHEN monitoramento externo consultar THEN dados SHALL estar sempre atualizados