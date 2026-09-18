namespace domain.Exceptions;

public abstract class AppException : Exception
{
    public abstract int StatusCode { get; }
    
    protected AppException(string message) : base(message) { }
}