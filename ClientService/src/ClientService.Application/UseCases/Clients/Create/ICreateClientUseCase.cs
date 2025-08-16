using ClientService.Application.Common;
using ClientService.Domain.Validations;

namespace ClientService.Application.UseCases.Clients.Create;

public interface ICreateClientUseCase : IUseCase<CreateClientInput, CreateClientOutput, Notification>
{
    
}