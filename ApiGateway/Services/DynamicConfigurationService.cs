using ApiGateway.Configuration;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Text.Json;

namespace ApiGateway.Services;

/// <summary>
/// Serviço para gerenciamento dinâmico de configurações do API Gateway
/// Permite alterar configurações em tempo de execução sem reiniciar o serviço
/// </summary>
public class DynamicConfigurationService : IConfigurationService, IDisposable
{
    private readonly IDatabase _database;
    private readonly ISubscriber _subscriber;
    private readonly IOptionsMonitor<ServiceDiscoveryOptions> _serviceDiscoveryOptions;
    private readonly ILogger<DynamicConfigurationService> _logger;
    private readonly HttpClient _httpClient;
    private readonly Timer _healthCheckTimer;
    
    // Constantes para chaves Redis e canais de comunicação
    private const string ConfigurationKey = "api-gateway:configuration";
    private const string ConfigurationChannel = "api-gateway:config-changes";
    
    /// <summary>
    /// Evento disparado quando a configuração muda
    /// </summary>
    public event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;

    /// <summary>
    /// Construtor - inicializa o serviço com suas dependências
    /// </summary>
    public DynamicConfigurationService(
        IConnectionMultiplexer redis,
        IOptionsMonitor<ServiceDiscoveryOptions> serviceDiscoveryOptions,
        ILogger<DynamicConfigurationService> logger,
        HttpClient httpClient)
    {
        _database = redis.GetDatabase();
        _subscriber = redis.GetSubscriber();
        _serviceDiscoveryOptions = serviceDiscoveryOptions;
        _logger = logger;
        _httpClient = httpClient;
        
        // Configura timer para health checks periódicos
        var healthCheckInterval = serviceDiscoveryOptions.CurrentValue.HealthCheckInterval;
        _healthCheckTimer = new Timer(PerformHealthChecks, null, healthCheckInterval, healthCheckInterval);
        
        // Inscreve-se em mudanças de configuração via Redis Pub/Sub
        _subscriber.Subscribe(RedisChannel.Literal(ConfigurationChannel), OnConfigurationChangeReceived);
        
        _logger.LogInformation("DynamicConfigurationService initialized with health check interval: {Interval}", healthCheckInterval);
    }

    /// <summary>
    /// Recarrega toda a configuração do gateway
    /// </summary>
    public async Task<bool> ReloadConfigurationAsync()
    {
        try
        {
            _logger.LogInformation("Reloading gateway configuration");
            
            // Obtém a configuração atual do appsettings
            var currentOptions = _serviceDiscoveryOptions.CurrentValue;
            
            // Serializa e salva no Redis
            var configJson = JsonSerializer.Serialize(currentOptions);
            await _database.StringSetAsync(ConfigurationKey, configJson);
            
            // Publica notificação de mudança
            await _subscriber.PublishAsync(RedisChannel.Literal(ConfigurationChannel), "Configuration reloaded");
            
            // Dispara evento local
            ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs
            {
                ServiceName = "*",
                ChangeType = "Reload",
                Timestamp = DateTime.UtcNow
            });
            
            _logger.LogInformation("Configuration reloaded successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reload configuration");
            return false;
        }
    }

    /// <summary>
    /// Atualiza ou adiciona um endpoint de serviço
    /// </summary>
    public async Task<bool> UpdateServiceEndpointAsync(string serviceName, string endpoint, int weight = 100)
    {
        try
        {
            _logger.LogInformation("Updating service endpoint: {ServiceName} -> {Endpoint}", serviceName, endpoint);
            
            // Obtém a configuração atual
            var currentConfigJson = await _database.StringGetAsync(ConfigurationKey);
            ServiceDiscoveryOptions currentConfig;
            
            if (currentConfigJson.HasValue)
            {
                currentConfig = JsonSerializer.Deserialize<ServiceDiscoveryOptions>(currentConfigJson!) ?? new ServiceDiscoveryOptions();
            }
            else
            {
                // Se não existe configuração no Redis, usa a do appsettings
                currentConfig = _serviceDiscoveryOptions.CurrentValue;
            }
            
            // Atualiza ou adiciona o serviço
            currentConfig.Services[serviceName] = new ServiceEndpoint
            {
                BaseUrl = endpoint,
                HealthEndpoint = "/health",
                Weight = weight,
                Metadata = new Dictionary<string, string>
                {
                    ["LastUpdated"] = DateTime.UtcNow.ToString("O")
                }
            };
            
            // Salva a configuração atualizada
            var configJson = JsonSerializer.Serialize(currentConfig);
            await _database.StringSetAsync(ConfigurationKey, configJson);
            
            // Publica notificação de mudança
            await _subscriber.PublishAsync(RedisChannel.Literal(ConfigurationChannel), $"Updated service: {serviceName}");
            
            // Dispara evento local
            ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs
            {
                ServiceName = serviceName,
                ChangeType = "Update",
                Timestamp = DateTime.UtcNow
            });
            
            _logger.LogInformation("Service endpoint updated successfully: {ServiceName}", serviceName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update service endpoint: {ServiceName}", serviceName);
            return false;
        }
    }

    /// <summary>
    /// Remove um serviço da configuração
    /// </summary>
    public async Task<bool> RemoveServiceEndpointAsync(string serviceName)
    {
        try
        {
            _logger.LogInformation("Removing service endpoint: {ServiceName}", serviceName);
            
            // Obtém a configuração atual
            var currentConfigJson = await _database.StringGetAsync(ConfigurationKey);
            
            if (!currentConfigJson.HasValue)
            {
                _logger.LogWarning("No configuration found to remove service: {ServiceName}", serviceName);
                return false;
            }
            
            // Deserializa a configuração atual
            var currentConfig = JsonSerializer.Deserialize<ServiceDiscoveryOptions>(currentConfigJson!) ?? new ServiceDiscoveryOptions();
            
            // Remove o serviço se existir
            if (currentConfig.Services.ContainsKey(serviceName))
            {
                currentConfig.Services.Remove(serviceName);
                
                // Serializa a configuração atualizada
                var configJson = JsonSerializer.Serialize(currentConfig);
                await _database.StringSetAsync(ConfigurationKey, configJson);
                
                // Publica notificação de mudança
                await _subscriber.PublishAsync(RedisChannel.Literal(ConfigurationChannel), $"Removed service: {serviceName}");
                
                // Dispara evento local
                ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs
                {
                    ServiceName = serviceName,
                    ChangeType = "Remove",
                    Timestamp = DateTime.UtcNow
                });
                
                _logger.LogInformation("Service endpoint removed successfully: {ServiceName}", serviceName);
                return true;
            }
            
            _logger.LogWarning("Service not found for removal: {ServiceName}", serviceName);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove service endpoint: {ServiceName}", serviceName);
            return false;
        }
    }

    /// <summary>
    /// Obtém a configuração atual de todos os serviços
    /// </summary>
    public async Task<Dictionary<string, object>> GetCurrentConfigurationAsync()
    {
        try
        {
            // Tenta obter do Redis primeiro
            var configJson = await _database.StringGetAsync(ConfigurationKey);
            ServiceDiscoveryOptions config;
            
            if (configJson.HasValue)
            {
                config = JsonSerializer.Deserialize<ServiceDiscoveryOptions>(configJson!) ?? new ServiceDiscoveryOptions();
            }
            else
            {
                // Se não existe no Redis, usa a configuração padrão e salva
                config = _serviceDiscoveryOptions.CurrentValue;
                var defaultConfigJson = JsonSerializer.Serialize(config);
                await _database.StringSetAsync(ConfigurationKey, defaultConfigJson);
            }
            
            // Converte para dicionário para retorno
            return ConvertToDictionary(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current configuration");
            // Retorna configuração padrão em caso de erro
            return ConvertToDictionary(_serviceDiscoveryOptions.CurrentValue);
        }
    }

    /// <summary>
    /// Converte ServiceDiscoveryOptions para Dictionary para serialização
    /// </summary>
    private Dictionary<string, object> ConvertToDictionary(ServiceDiscoveryOptions options)
    {
        return new Dictionary<string, object>
        {
            ["Services"] = options.Services.ToDictionary(
                kvp => kvp.Key,
                kvp => new
                {
                    kvp.Value.BaseUrl,
                    kvp.Value.HealthEndpoint,
                    kvp.Value.Weight,
                    kvp.Value.Metadata
                }
            ),
            ["RefreshInterval"] = options.RefreshInterval.ToString(),
            ["HealthCheckInterval"] = options.HealthCheckInterval.ToString(),
            ["LastUpdated"] = DateTime.UtcNow.ToString("O"),
            ["TotalServices"] = options.Services.Count
        };
    }

    /// <summary>
    /// Manipula notificações de mudança de configuração via Redis
    /// </summary>
    private void OnConfigurationChangeReceived(RedisChannel channel, RedisValue message)
    {
        _logger.LogInformation("Configuration change received: {Message}", message);
        
        // Dispara evento local para notificar outros componentes
        ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs
        {
            ServiceName = "*",
            ChangeType = "External",
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Executa verificações de saúde periódicas nos serviços
    /// </summary>
    private async void PerformHealthChecks(object? state)
    {
        try
        {
            var config = await GetCurrentConfigurationAsync();
            
            if (config.TryGetValue("Services", out var servicesObj) && 
                servicesObj is Dictionary<string, object> services)
            {
                var healthCheckTasks = services.Select(async kvp =>
                {
                    var serviceName = kvp.Key;
                    
                    try
                    {
                        // Extrai informações do serviço
                        if (kvp.Value is not Dictionary<string, object> serviceInfo ||
                            !serviceInfo.TryGetValue("BaseUrl", out var baseUrlObj) ||
                            !serviceInfo.TryGetValue("HealthEndpoint", out var healthEndpointObj))
                        {
                            return;
                        }
                        
                        var baseUrl = baseUrlObj.ToString();
                        var healthEndpoint = healthEndpointObj.ToString();
                        var healthUrl = $"{baseUrl}{healthEndpoint}";
                        
                        // Executa health check com timeout
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                        var response = await _httpClient.GetAsync(healthUrl, cts.Token);
                        
                        if (!response.IsSuccessStatusCode)
                        {
                            _logger.LogWarning("Health check failed for {ServiceName}: {StatusCode}", 
                                serviceName, response.StatusCode);
                            
                            // Considera remover serviços não saudáveis após várias falhas
                            // Implementação de circuit breaker pode ser adicionada aqui
                        }
                        else
                        {
                            _logger.LogDebug("Health check passed for {ServiceName}", serviceName);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Health check error for {ServiceName}", serviceName);
                    }
                });
                
                await Task.WhenAll(healthCheckTasks);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during health checks");
        }
    }

    /// <summary>
    /// Libera recursos utilizados pelo serviço
    /// </summary>
    public void Dispose()
    {
        _healthCheckTimer?.Dispose();
        _httpClient?.Dispose();
        
        // Desinscreve-se do canal Redis
        try
        {
            _subscriber?.Unsubscribe(RedisChannel.Literal(ConfigurationChannel));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error unsubscribing from Redis channel");
        }
        
        _logger.LogInformation("DynamicConfigurationService disposed");
    }
}