namespace ClientService.Domain.Validations;

public interface IValidationHandler
{
    IValidationHandler Add(Error error);
    IValidationHandler Add(IValidationHandler handler);
    IReadOnlyList<Error> Errors { get; }
    bool HasErrors { get; }
}