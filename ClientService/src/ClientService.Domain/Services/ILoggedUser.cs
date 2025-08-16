using ClientService.Domain.Aggregates;

namespace ClientService.Domain.Services;

public interface ILoggedUser
{
    public Task<Client?> User();
}