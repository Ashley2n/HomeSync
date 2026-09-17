namespace domain.Exceptions;

public class ValidationException : AppException
{
    public override int StatusCode => 400;
    
    public ValidationException (string message) : base(message) { }
    
    public ValidationException(string resourceName, string  key) :
        base($"{resourceName} fails a business rule with key: {key}") {}
}