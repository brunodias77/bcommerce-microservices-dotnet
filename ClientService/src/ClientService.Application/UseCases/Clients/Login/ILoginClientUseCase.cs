using ClientService.Application.Common;
using ClientService.Domain.Validations;

namespace ClientService.Application.UseCases.Clients.Login;

public interface ILoginClientUseCase : IUseCase<LoginClientInput, LoginClientOutput, Notification>
{
}