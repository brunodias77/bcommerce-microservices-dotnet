namespace ClientService.Application.Common;

public interface IUseCase<in TInput, TSuccess, TError>
{
    Task<Result<TSuccess, TError>> Execute(TInput input);

}