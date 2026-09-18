namespace domain.Exceptions;

public class NotFoundException : AppException
{

    public override int StatusCode => 404;
    
    /// <summary>
    /// throw new NotFoundException("Example Message");
    /// </summary>
    public NotFoundException(string message) : base(message) { }
    
    /// <summary>
    /// e.g throw new NotFoundException(nameof(Household), householdId);
    /// </summary>
    public NotFoundException(string resourceName, object key) : 
        base($"{resourceName} with id {key} was not found.") { }
}