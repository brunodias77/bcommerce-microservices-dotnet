namespace ApiGateway.Configuration;

/// <summary>
/// Classe que define as opções de configuração para descoberta de serviços
/// Esta classe é mapeada automaticamente a partir do appsettings.json
/// </summary>
public class ServiceDiscoveryOptions
{
    /// <summary>
    /// Nome da seção no appsettings.json onde estão as configurações
    /// Usado pelo sistema de configuração do .NET para mapear automaticamente
    /// </summary>
    public const string SectionName = "ServiceDiscovery";
    
    /// <summary>
    /// Intervalo entre atualizações da configuração de serviços
    /// Padrão: 1 minuto
    /// </summary>
    public TimeSpan RefreshInterval { get; set; } = TimeSpan.FromMinutes(1);
    
    /// <summary>
    /// Intervalo entre verificações de saúde dos serviços
    /// Padrão: 30 segundos
    /// </summary>
    public TimeSpan HealthCheckInterval { get; set; } = TimeSpan.FromSeconds(30);
    
    /// <summary>
    /// Dicionário com informações de todos os serviços conhecidos
    /// Chave: nome do serviço (ex: "client-service")
    /// Valor: configurações do endpoint do serviço
    /// </summary>
    public Dictionary<string, ServiceEndpoint> Services { get; set; } = new();
}

/// <summary>
/// Representa as informações de um endpoint de serviço
/// Contém todas as informações necessárias para se conectar a um microserviço
/// </summary>
public class ServiceEndpoint
{
    /// <summary>
    /// URL base do serviço (ex: "http://localhost:5122")
    /// Usado para construir as URLs completas das requisições
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// Endpoint para verificação de saúde do serviço
    /// Padrão: "/health" - endpoint padrão do ASP.NET Core Health Checks
    /// </summary>
    public string HealthEndpoint { get; set; } = "/health";
    
    /// <summary>
    /// Peso do serviço para balanceamento de carga
    /// Valores maiores recebem mais tráfego
    /// Padrão: 100 (peso normal)
    /// </summary>
    public int Weight { get; set; } = 100;
    
    /// <summary>
    /// Metadados adicionais do serviço
    /// Pode conter informações como versão, região, etc.
    /// Usado para roteamento avançado e monitoramento
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}