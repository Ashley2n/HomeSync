namespace domain.Exceptions;

public class ValidationException : AppException
{
    public override int StatusCode => 400;
    
    /// <summary>
    /// throw new ValidationException("Example Message");
    /// </summary>
    public ValidationException (string message) : base(message) { }

    /// <summary>
    /// throw new ValidationException("InviteCode", "has expired.");
    /// </summary>
    public ValidationException(string resourceName, object reason) :
        base($"{resourceName} fails a business rule with key: {reason}") {}
}