using ClientService.Domain.Aggregates;
using ClientService.Domain.Repositories;
using ClientService.Domain.ValueObjects;
using ClientService.Infra.Data;

namespace ClientService.Infra.Repositories;

public class ClientRepository : IClientRepository
{
    
    private readonly ClientDbContext _context;

    public ClientRepository(ClientDbContext context)
    {
        _context = context;
    }
    
    public Task Insert(Client aggregate, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<Client?> Get(Guid id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task Delete(Client aggregate, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task Update(Client aggregate, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<Client?> GetByIdAsync(Guid clientId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Client?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Client?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Client?> GetByCpfAsync(Cpf cpf, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ExistsByCpfAsync(Cpf cpf, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task AddAsync(Client client, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task UpdateAsync(Client client, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(Client client, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}