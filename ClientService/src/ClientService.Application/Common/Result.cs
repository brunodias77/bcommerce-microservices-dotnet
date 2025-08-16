namespace ClientService.Application.Common;

public sealed class Result<TSuccess, TError>
{
    public bool IsSuccess { get; }
    public TSuccess? Value { get; }
    public TError? Error { get; }

    private Result(bool isSuccess, TSuccess? value, TError? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }
    
    public static Result<TSuccess, TError> Ok(TSuccess value)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));
        return new Result<TSuccess, TError>(true, value, default);
    }
    
    public static Result<TSuccess, TError> Fail(TError error)
    {
        if (error == null) throw new ArgumentNullException(nameof(error));
        return new Result<TSuccess, TError>(false, default, error);
    }
    
        public override string ToString()
        {
            return IsSuccess
                ? $"Success: {Value}"
                : $"Error: {Error}";
        }
}