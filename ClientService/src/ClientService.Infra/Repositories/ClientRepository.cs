using ClientService.Domain.Aggregates;
using ClientService.Domain.Repositories;
using ClientService.Domain.ValueObjects;
using ClientService.Infra.Data;
using Microsoft.EntityFrameworkCore;

namespace ClientService.Infra.Repositories;

public class ClientRepository : IClientRepository
{
    private readonly ClientDbContext _context;

    public ClientRepository(ClientDbContext context)
    {
        _context = context;
    }

    // Métodos da interface base IRepository<Client>
    public async Task Insert(Client aggregate, CancellationToken cancellationToken)
    {
        await _context.Clients.AddAsync(aggregate, cancellationToken);
    }

    public async Task<Client?> Get(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Clients
            .Include(c => c.Addresses)
            .Include(c => c.Consents)
            .Include(c => c.SavedCards)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public Task Delete(Client aggregate, CancellationToken cancellationToken)
    {
        // Soft delete - apenas marca como deletado
        aggregate.MarkAsDeleted();
        return Task.CompletedTask;
    }

    public Task Update(Client aggregate, CancellationToken cancellationToken)
    {
        _context.Clients.Update(aggregate);
        return Task.CompletedTask;
    }

    // Métodos específicos da interface IClientRepository
    public async Task<Client?> GetByIdAsync(Guid clientId, CancellationToken cancellationToken = default)
    {
        return await _context.Clients
            .Include(c => c.Addresses)
            .Include(c => c.Consents)
            .Include(c => c.SavedCards)
            .FirstOrDefaultAsync(c => c.Id == clientId, cancellationToken);
    }

    public async Task<Client?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        return await _context.Clients
            .Include(c => c.Addresses)
            .Include(c => c.Consents)
            .Include(c => c.SavedCards)
            .FirstOrDefaultAsync(c => c.Email == email, cancellationToken);
    }

    public async Task<Client?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var emailValueObject = Domain.ValueObjects.Email.Create(email);
        return await _context.Clients
            .Include(c => c.Addresses)
            .Include(c => c.Consents)
            .Include(c => c.SavedCards)
            .FirstOrDefaultAsync(c => c.Email == emailValueObject, cancellationToken);
    }

    public async Task<Client?> GetByCpfAsync(Cpf cpf, CancellationToken cancellationToken = default)
    {
        return await _context.Clients
            .Include(c => c.Addresses)
            .Include(c => c.Consents)
            .Include(c => c.SavedCards)
            .FirstOrDefaultAsync(c => c.Cpf == cpf, cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        return await _context.Clients
            .AnyAsync(c => c.Email == email, cancellationToken);
    }

    public async Task<bool> ExistsByCpfAsync(Cpf cpf, CancellationToken cancellationToken = default)
    {
        return await _context.Clients
            .AnyAsync(c => c.Cpf == cpf, cancellationToken);
    }

    public async Task AddAsync(Client client, CancellationToken cancellationToken = default)
    {
        await _context.Clients.AddAsync(client, cancellationToken);
    }

    public Task UpdateAsync(Client client, CancellationToken cancellationToken = default)
    {
        _context.Clients.Update(client);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Client client, CancellationToken cancellationToken = default)
    {
        // Soft delete - apenas marca como deletado
        client.MarkAsDeleted();
        return Task.CompletedTask;
    }
}