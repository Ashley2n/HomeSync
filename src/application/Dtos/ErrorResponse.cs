namespace application.Dtos;

public record ErrorResponse(string Message, int StatusCode, string RequestId );