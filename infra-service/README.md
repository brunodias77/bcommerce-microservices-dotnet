# B-Commerce Infrastructure Service

## Visão Geral

O Infra Service é responsável por provisionar e configurar toda a infraestrutura base compartilhada do B-Commerce, incluindo:

- **PostgreSQL**: Banco de dados principal e banco dedicado para Keycloak
- **Keycloak**: Servidor de autenticação e autorização (OIDC/OAuth2)
- **RabbitMQ**: Message broker para comunicação assíncrona entre microsserviços

## Arquitetura