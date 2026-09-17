namespace domain.Exceptions;

public class ForbiddenException : AppException
{
    public override int StatusCode => 403;
    
    public ForbiddenException(string message) : base(message) { }

    public ForbiddenException(string resourceName, string key) :
        base($"{resourceName} is forbidden. {key}") {}
}