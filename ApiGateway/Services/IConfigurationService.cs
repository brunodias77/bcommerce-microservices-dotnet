namespace ApiGateway.Services;

/// <summary>
/// Interface que define o contrato para gerenciamento dinâmico de configurações
/// Permite alterar configurações do gateway em tempo de execução sem reiniciar
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Recarrega toda a configuração do gateway
    /// Útil quando há mudanças globais na configuração
    /// </summary>
    /// <returns>True se recarregou com sucesso, False caso contrário</returns>
    Task<bool> ReloadConfigurationAsync();
    
    /// <summary>
    /// Atualiza ou adiciona um endpoint de serviço
    /// Permite adicionar novos serviços ou alterar URLs existentes dinamicamente
    /// </summary>
    /// <param name="serviceName">Nome do serviço (ex: "client-service")</param>
    /// <param name="endpoint">Nova URL do serviço</param>
    /// <param name="weight">Peso para balanceamento de carga (padrão: 100)</param>
    /// <returns>True se atualizou com sucesso, False caso contrário</returns>
    Task<bool> UpdateServiceEndpointAsync(string serviceName, string endpoint, int weight = 100);
    
    /// <summary>
    /// Remove um serviço da configuração
    /// Usado quando um serviço é desativado ou removido
    /// </summary>
    /// <param name="serviceName">Nome do serviço a ser removido</param>
    /// <returns>True se removeu com sucesso, False caso contrário</returns>
    Task<bool> RemoveServiceEndpointAsync(string serviceName);
    
    /// <summary>
    /// Obtém a configuração atual de todos os serviços
    /// Inclui status de saúde, URLs, pesos, etc.
    /// </summary>
    /// <returns>Dicionário com a configuração atual</returns>
    Task<Dictionary<string, object>> GetCurrentConfigurationAsync();
    
    /// <summary>
    /// Evento disparado quando a configuração muda
    /// Permite que outros componentes sejam notificados de mudanças
    /// </summary>
    event EventHandler<ConfigurationChangedEventArgs> ConfigurationChanged;
}

/// <summary>
/// Argumentos do evento de mudança de configuração
/// Contém informações sobre o que mudou
/// </summary>
public class ConfigurationChangedEventArgs : EventArgs
{
    /// <summary>
    /// Nome do serviço que teve a configuração alterada
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;
    
    /// <summary>
    /// Tipo de mudança: "Update", "Remove", "Reload", etc.
    /// </summary>
    public string ChangeType { get; set; } = string.Empty;
    
    /// <summary>
    /// Timestamp de quando a mudança ocorreu
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}