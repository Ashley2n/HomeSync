namespace domain.Exceptions;

public class ConflictException : AppException
{
    
    public override int StatusCode => 409;
    
    /// <summary>
    /// throw new ConflictException("Example Message");
    /// </summary>
    public ConflictException(string message) : base(message) { }

    /// <summary>
    /// throw new ConflictException(nameof(Household), householdId);
    /// </summary>
    public ConflictException(string resourceName, object key)
        : base($"{resourceName} conflicts with key {key}") {}
}
