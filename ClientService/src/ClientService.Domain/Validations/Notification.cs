namespace ClientService.Domain.Validations;

public class Notification : IValidationHandler
{
    private readonly List<Error> _errors = new();
    public IReadOnlyList<Error> Errors => _errors;
    public bool HasErrors => _errors.Count > 0;
    
    
    public IValidationHandler Add(Error error)
    {
        _errors.Add(error);
        return this;
    }

    public IValidationHandler Add(IValidationHandler handler)
    {
        _errors.AddRange(handler.Errors);
        return this;
    }

  
}