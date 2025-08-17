using ClientService.Domain.Aggregates;
using ClientService.Domain.Common;
using ClientService.Domain.ValueObjects;

namespace ClientService.Domain.Repositories;

public interface IClientRepository : IRepository<Client>
{
    Task<Client?> GetByIdAsync(Guid clientId, CancellationToken cancellationToken = default);
    Task<Client?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default);
    Task<Client?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<Client?> GetByCpfAsync(Cpf cpf, CancellationToken cancellationToken = default);
    Task<Client?> GetByKeycloakUserIdAsync(string keycloakUserId, CancellationToken cancellationToken = default); // Adicionar esta linha
    Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken = default);
    Task<bool> ExistsByCpfAsync(Cpf cpf, CancellationToken cancellationToken = default);
    Task AddAsync(Client client, CancellationToken cancellationToken = default);
    Task UpdateAsync(Client client, CancellationToken cancellationToken = default);
    Task DeleteAsync(Client client, CancellationToken cancellationToken = default);
}