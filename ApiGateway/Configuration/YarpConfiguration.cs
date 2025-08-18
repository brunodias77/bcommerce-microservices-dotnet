using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Forwarder;
using Yarp.ReverseProxy.Health;
using Yarp.ReverseProxy.LoadBalancing;
using Yarp.ReverseProxy.Transforms;

namespace ApiGateway.Configuration;

/// <summary>
/// Provedor de configuração personalizado para YARP (Yet Another Reverse Proxy)
/// Esta classe implementa IProxyConfigProvider para fornecer configurações dinâmicas
/// de roteamento, clusters e políticas para o reverse proxy
/// </summary>
public class YarpConfiguration : IProxyConfigProvider
{
    // Provedor de configuração em memória do YARP
    private readonly InMemoryConfigProvider _inMemoryConfigProvider;

    // Monitor das opções de descoberta de serviços
    private readonly IOptionsMonitor<ServiceDiscoveryOptions> _serviceDiscoveryOptionsMonitor;

    // Monitor das configurações gerais da aplicação
    private readonly IOptionsMonitor<IConfiguration> _configurationMonitor;

    /// <summary>
    /// Construtor da configuração YARP
    /// </summary>
    /// <param name="serviceDiscoveryOptionsMonitor">Monitor das opções de descoberta de serviços</param>
    /// <param name="configurationMonitor">Monitor das configurações da aplicação</param>
    public YarpConfiguration(
        IOptionsMonitor<ServiceDiscoveryOptions> serviceDiscoveryOptionsMonitor,
        IOptionsMonitor<IConfiguration> configurationMonitor)
    {
        _serviceDiscoveryOptionsMonitor = serviceDiscoveryOptionsMonitor;
        _configurationMonitor = configurationMonitor;

        // Inicializa o provedor de configuração em memória
        _inMemoryConfigProvider = new InMemoryConfigProvider(GetRoutes(), GetClusters());

        // Registra callback para mudanças nas opções de descoberta de serviços
        _serviceDiscoveryOptionsMonitor.OnChange(OnConfigurationChanged);
    }

    /// <summary>
    /// Obtém a configuração atual do proxy
    /// Este método é chamado pelo YARP para obter as configurações de roteamento
    /// </summary>
    /// <returns>Configuração do proxy com rotas e clusters</returns>
    public IProxyConfig GetConfig()
    {
        return _inMemoryConfigProvider.GetConfig();
    }

    /// <summary>
    /// Callback executado quando a configuração de descoberta de serviços muda
    /// Atualiza as configurações do YARP com base nas novas configurações
    /// </summary>
    /// <param name="options">Novas opções de descoberta de serviços</param>
    private void OnConfigurationChanged(ServiceDiscoveryOptions options)
    {
        // Atualiza o provedor em memória com novas rotas e clusters
        _inMemoryConfigProvider.Update(GetRoutes(), GetClusters());

        // Nota: InMemoryConfigProvider não implementa IDisposable
        // então não é necessário chamar Dispose()
        // oldConfigProvider.Dispose(); // Removido - não é necessário
    }

    /// <summary>
    /// Obtém as rotas configuradas para o proxy
    /// Cada rota define como as requisições são direcionadas para os clusters
    /// </summary>
    /// <returns>Lista de configurações de rota</returns>
    private IReadOnlyList<RouteConfig> GetRoutes()
    {
        var routes = new List<RouteConfig>();
        var configuration = _configurationMonitor.CurrentValue;

        // Obtém seção de rotas do appsettings.json
        var routesSection = configuration.GetSection("ReverseProxy:Routes");

        foreach (var routeSection in routesSection.GetChildren())
        {
            // Cria configuração de rota baseada no appsettings
            var route = new RouteConfig
            {
                RouteId = routeSection.Key,
                ClusterId = routeSection["ClusterId"] ?? string.Empty, // CORRIGIDO
                Match = new RouteMatch
                {
                    Path = routeSection["Match:Path"] ?? "/"
                },
                Transforms = GetTransforms(routeSection.GetSection("Transforms")),
                Metadata = GetMetadata(routeSection.GetSection("Metadata"))
            };
            routes.Add(route);
        }

        return routes;
    }

    /// <summary>
    /// Obtém os clusters configurados para o proxy
    /// Cada cluster representa um grupo de destinos (endpoints) para um serviço
    /// </summary>
    /// <returns>Lista de configurações de cluster</returns>
    private IReadOnlyList<ClusterConfig> GetClusters()
    {
        var clusters = new List<ClusterConfig>();
        var configuration = _configurationMonitor.CurrentValue;

        // Obtém seção de clusters do appsettings.json
        var clustersSection = configuration.GetSection("ReverseProxy:Clusters");

        foreach (var clusterSection in clustersSection.GetChildren())
        {
            // Cria configuração de cluster
            var cluster = new ClusterConfig
            {
                // ID único do cluster
                ClusterId = clusterSection.Key,

                // Política de balanceamento de carga
                LoadBalancingPolicy = LoadBalancingPolicies.RoundRobin,

                // Destinos (endpoints) do cluster
                Destinations = GetDestinations(clusterSection.GetSection("Destinations")),

                // Configurações de health check
                HealthCheck = GetHealthCheckConfig(clusterSection.GetSection("HealthCheck")),

                // Configurações de requisição HTTP
                HttpRequest = GetForwarderRequestConfig(clusterSection.GetSection("HttpRequest")),

                // Metadados do cluster
                Metadata = GetMetadata(clusterSection.GetSection("Metadata"))
            };

            clusters.Add(cluster);
        }

        return clusters;
    }

    /// <summary>
    /// Obtém os destinos (endpoints) de um cluster
    /// Cada destino representa uma instância específica de um serviço
    /// </summary>
    /// <param name="destinationsSection">Seção de configuração dos destinos</param>
    /// <returns>Dicionário de destinos</returns>
    private IReadOnlyDictionary<string, DestinationConfig> GetDestinations(IConfigurationSection destinationsSection)
    {
        var destinations = new Dictionary<string, DestinationConfig>();

        foreach (var destinationSection in destinationsSection.GetChildren())
        {
            // Cria configuração de destino
            var destination = new DestinationConfig
            {
                // URL base do serviço
                Address = destinationSection["Address"],

                // Endpoint específico para health check
                Health = destinationSection["Health"],

                // Metadados do destino (peso, tags, etc.)
                Metadata = GetMetadata(destinationSection.GetSection("Metadata"))
            };

            destinations[destinationSection.Key] = destination;
        }

        return destinations;
    }

    /// <summary>
    /// Obtém configurações de health check para um cluster
    /// Define como e quando verificar se os serviços estão saudáveis
    /// </summary>
    /// <param name="healthCheckSection">Seção de configuração de health check</param>
    /// <returns>Configuração de health check</returns>
    private HealthCheckConfig? GetHealthCheckConfig(IConfigurationSection healthCheckSection)
    {
        if (!healthCheckSection.Exists())
            return null;

        return new HealthCheckConfig
        {
            // Health check ativo (o proxy verifica proativamente)
            Active = GetActiveHealthCheckConfig(healthCheckSection.GetSection("Active")),

            // Health check passivo (baseado em falhas de requisições)
            Passive = GetPassiveHealthCheckConfig(healthCheckSection.GetSection("Passive"))
        };
    }

    /// <summary>
    /// Configurações de health check ativo
    /// O proxy faz requisições periódicas para verificar a saúde dos serviços
    /// </summary>
    /// <param name="activeSection">Seção de configuração ativa</param>
    /// <returns>Configuração de health check ativo</returns>
    private ActiveHealthCheckConfig? GetActiveHealthCheckConfig(IConfigurationSection activeSection)
    {
        if (!activeSection.Exists())
            return null;

        return new ActiveHealthCheckConfig
        {
            // Se o health check ativo está habilitado
            Enabled = activeSection.GetValue<bool>("Enabled"),

            // Intervalo entre verificações
            Interval = activeSection.GetValue<TimeSpan?>("Interval"),

            // Timeout para cada verificação
            Timeout = activeSection.GetValue<TimeSpan?>("Timeout"),

            // Política de health check (Any, All, etc.)
            Policy = activeSection["Policy"],

            // Caminho do endpoint de health check
            Path = activeSection["Path"]
        };
    }

    /// <summary>
    /// Configurações de health check passivo
    /// Monitora falhas de requisições normais para determinar saúde dos serviços
    /// </summary>
    /// <param name="passiveSection">Seção de configuração passiva</param>
    /// <returns>Configuração de health check passivo</returns>
    private PassiveHealthCheckConfig? GetPassiveHealthCheckConfig(IConfigurationSection passiveSection)
    {
        if (!passiveSection.Exists())
            return null;

        return new PassiveHealthCheckConfig
        {
            // Se o health check passivo está habilitado
            Enabled = passiveSection.GetValue<bool>("Enabled"),

            // Política de health check passivo
            Policy = passiveSection["Policy"],

            // Período de tempo para reativar destino não saudável
            ReactivationPeriod = passiveSection.GetValue<TimeSpan?>("ReactivationPeriod")
        };
    }

    /// <summary>
    /// Obtém configurações de requisição HTTP para forwarding
    /// Define timeouts, versão HTTP, e outras configurações de requisição
    /// </summary>
    /// <param name="httpRequestSection">Seção de configuração HTTP</param>
    /// <returns>Configuração de requisição</returns>
    private ForwarderRequestConfig? GetForwarderRequestConfig(IConfigurationSection httpRequestSection)
    {
        if (!httpRequestSection.Exists())
            return null;

        return new ForwarderRequestConfig
        {
            // Timeout para atividade da requisição (substitui o antigo Timeout)
            ActivityTimeout = httpRequestSection.GetValue<TimeSpan?>("ActivityTimeout"),

            // Versão HTTP a ser usada
            Version = httpRequestSection.GetValue<Version>("Version"),

            // Política de versão HTTP
            VersionPolicy = httpRequestSection.GetValue<HttpVersionPolicy?>("VersionPolicy")
        };
    }

    /// <summary>
    /// Obtém transformações aplicadas às requisições
    /// Permite modificar headers, paths, query strings, etc.
    /// </summary>
    /// <param name="transformsSection">Seção de configuração de transformações</param>
    /// <returns>Lista de transformações</returns>
    private IReadOnlyList<IReadOnlyDictionary<string, string>>? GetTransforms(IConfigurationSection transformsSection)
    {
        if (!transformsSection.Exists())
            return null;

        var transforms = new List<IReadOnlyDictionary<string, string>>();

        foreach (var transformSection in transformsSection.GetChildren())
        {
            var transform = new Dictionary<string, string>();

            // Adiciona todas as propriedades da transformação
            foreach (var kvp in transformSection.AsEnumerable())
            {
                if (kvp.Value != null)
                {
                    transform[kvp.Key] = kvp.Value;
                }
            }

            transforms.Add(transform);
        }

        return transforms;
    }

    /// <summary>
    /// Obtém metadados de configuração
    /// Metadados são informações adicionais que podem ser usadas por middlewares
    /// </summary>
    /// <param name="metadataSection">Seção de metadados</param>
    /// <returns>Dicionário de metadados</returns>
    private IReadOnlyDictionary<string, string>? GetMetadata(IConfigurationSection metadataSection)
    {
        if (!metadataSection.Exists())
            return null;

        var metadata = new Dictionary<string, string>();

        // Adiciona todos os metadados da seção
        foreach (var kvp in metadataSection.AsEnumerable())
        {
            if (kvp.Value != null)
            {
                metadata[kvp.Key] = kvp.Value;
            }
        }

        return metadata;
    }
}