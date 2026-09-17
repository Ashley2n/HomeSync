namespace domain.Exceptions;

public class NotFoundException : AppException
{
    /// <summary>
    /// throw new NotFoundException(nameof(Household), householdId);
    /// </summary>
    public override int StatusCode => 404;
    
    public NotFoundException(string message) : base(message) { }
    
    public NotFoundException(string resourceName, object key) : 
        base($"{resourceName} with id  {key} was not found.") { }
}