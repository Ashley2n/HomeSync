namespace domain.Exceptions;

public class ConcurrencyConflictException : AppException
{
    public override int StatusCode => 409;
    
    /// <summary>
    /// throw new ConcurrencyConflictException("Example Message");
    /// </summary>
    public ConcurrencyConflictException(string message) : base(message) { }

    /// <summary>
    /// throw new ConcurrencyConflictException(nameof(Household), householdId);
    /// </summary>
    public ConcurrencyConflictException(string resourceName, object key)
        : base($"{resourceName} conflicts with key {key}") {}
}