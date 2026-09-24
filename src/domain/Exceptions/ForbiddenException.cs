namespace domain.Exceptions;

public class ForbiddenException : AppException
{
    public override int StatusCode => 403;
    /// <summary>
    /// throw new ForbiddenException("Example Message");
    /// </summary>
    public ForbiddenException(string message) : base(message) { }
    /// <summary>
    /// throw new ForbiddenException("Remove member", "only the household owner can do this.");
    /// </summary>
    public ForbiddenException(string action, object key) :
        base($"{action} is forbidden. {key}") {}
}