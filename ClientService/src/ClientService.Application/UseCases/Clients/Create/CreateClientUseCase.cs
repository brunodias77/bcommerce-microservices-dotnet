using ClientService.Application.Common;
using ClientService.Domain.Validations;

namespace ClientService.Application.UseCases.Clients.Create;

public class CreateClientUseCase : ICreateClientUseCase
{
    public Task<Result<CreateClientOutput, Notification>> Execute(CreateClientInput input)
    {
        throw new NotImplementedException();
    }
}