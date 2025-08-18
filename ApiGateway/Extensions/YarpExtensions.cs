using ApiGateway.Configuration;
using ApiGateway.Services;
using Microsoft.Extensions.Options;
using Yarp.ReverseProxy.Configuration;

namespace ApiGateway.Extensions;

/// <summary>
/// Classe estática com métodos de extensão para configurar o YARP avançado
/// Métodos de extensão permitem adicionar funcionalidades a classes existentes
/// </summary>
public static class YarpExtensions
{
    /// <summary>
    /// Adiciona e configura o YARP com funcionalidades avançadas
    /// Este método estende IServiceCollection para facilitar a configuração
    /// </summary>
    /// <param name="services">Container de injeção de dependência</param>
    /// <param name="configuration">Configuração da aplicação</param>
    /// <returns>IServiceCollection para permitir method chaining</returns>
    public static IServiceCollection AddAdvancedYarp(this IServiceCollection services, IConfiguration configuration)
    {
        // CONFIGURAÇÃO DE SERVICE DISCOVERY
        // Registra as opções de descoberta de serviços no container DI
        services.Configure<ServiceDiscoveryOptions>(configuration.GetSection(ServiceDiscoveryOptions.SectionName));
        
        // CONFIGURAÇÃO DO YARP COM PROVEDOR PERSONALIZADO
        // Registra nossa implementação personalizada de configuração do YARP
        services.AddSingleton<YarpConfiguration>(); // Nossa classe de configuração personalizada
        services.AddSingleton<IProxyConfigProvider>(provider => provider.GetRequiredService<YarpConfiguration>()); // Interface do YARP
        services.AddReverseProxy(); // Adiciona os serviços básicos do YARP
        
        // SERVIÇO DE CONFIGURAÇÃO DINÂMICA
        // Permite alterar configurações em tempo de execução
        services.AddSingleton<IConfigurationService, DynamicConfigurationService>();
        
        // SERVIÇO DE MONITORAMENTO EM BACKGROUND
        // Monitora mudanças de configuração continuamente
        services.AddHostedService<ConfigurationMonitoringService>();
        
        return services; // Retorna para permitir method chaining
    }
}

/// <summary>
/// Serviço em background que monitora mudanças de configuração
/// BackgroundService roda continuamente em uma thread separada
/// </summary>
public class ConfigurationMonitoringService : BackgroundService
{
    private readonly ILogger<ConfigurationMonitoringService> _logger;
    private readonly IConfigurationService _configurationService;
    private readonly IOptionsMonitor<ServiceDiscoveryOptions> _serviceDiscoveryOptions;

    /// <summary>
    /// Construtor - recebe dependências via injeção de dependência
    /// </summary>
    public ConfigurationMonitoringService(
        ILogger<ConfigurationMonitoringService> logger,
        IConfigurationService configurationService,
        IOptionsMonitor<ServiceDiscoveryOptions> serviceDiscoveryOptions)
    {
        _logger = logger;
        _configurationService = configurationService;
        _serviceDiscoveryOptions = serviceDiscoveryOptions;
        
        // Inscreve-se no evento de mudança de configuração
        _configurationService.ConfigurationChanged += OnConfigurationChanged;
    }

    /// <summary>
    /// Método principal que executa continuamente em background
    /// </summary>
    /// <param name="stoppingToken">Token para cancelar a operação</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Configuration monitoring service started");
        
        // Loop infinito até a aplicação ser encerrada
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Obtém as opções atuais de service discovery
                var options = _serviceDiscoveryOptions.CurrentValue;
                
                // Aguarda o intervalo configurado antes da próxima verificação
                await Task.Delay(options.RefreshInterval, stoppingToken);
                
                // VALIDAÇÃO PERIÓDICA DA CONFIGURAÇÃO
                // Verifica se a configuração atual está válida
                var config = await _configurationService.GetCurrentConfigurationAsync();
                _logger.LogDebug("Configuration validation completed. Services: {ServiceCount}", config.Count);
            }
            catch (OperationCanceledException)
            {
                // Operação foi cancelada (aplicação sendo encerrada)
                break;
            }
            catch (Exception ex)
            {
                // Log de erro mas continua executando
                _logger.LogError(ex, "Error in configuration monitoring");
            }
        }
        
        _logger.LogInformation("Configuration monitoring service stopped");
    }

    /// <summary>
    /// Manipulador de evento chamado quando a configuração muda
    /// </summary>
    /// <param name="sender">Objeto que disparou o evento</param>
    /// <param name="e">Argumentos do evento com detalhes da mudança</param>
    private void OnConfigurationChanged(object? sender, ConfigurationChangedEventArgs e)
    {
        _logger.LogInformation("Configuration changed for service {ServiceName}: {ChangeType}", 
            e.ServiceName, e.ChangeType);
    }

    /// <summary>
    /// Método chamado quando o serviço é encerrado
    /// Remove a inscrição do evento para evitar vazamentos de memória
    /// </summary>
    public override void Dispose()
    {
        _configurationService.ConfigurationChanged -= OnConfigurationChanged;
        base.Dispose();
    }
}