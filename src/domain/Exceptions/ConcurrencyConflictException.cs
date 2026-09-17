namespace domain.Exceptions;

public class ConcurrencyConflictException : AppException
{
    public override int StatusCode => 409;
    
    public ConcurrencyConflictException (string message) : base(message) { }
    
    public ConcurrencyConflictException (string resourceName, string key)
        : base($"{resourceName} conflicts with key {key}") {}
}