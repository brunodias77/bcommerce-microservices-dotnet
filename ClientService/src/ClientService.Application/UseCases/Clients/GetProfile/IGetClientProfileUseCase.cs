using ClientService.Application.Common;
using ClientService.Domain.Validations;

namespace ClientService.Application.UseCases.Clients.GetProfile;

public interface IGetClientProfileUseCase
{
    Task<Result<GetClientProfileOutput, Notification>> Execute();
}