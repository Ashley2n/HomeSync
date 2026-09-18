using System.Text.Json;
using application.Dtos;
using domain.Exceptions;
using infrastructure.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace test.unit;

public class ExceptionHandlingMiddlewareTests
{
    private static async Task<(int status, ErrorResponse body, string requestId)> RunAsync(Exception toThrow)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var middleware =
            new ExceptionHandlingMiddleware(_ => throw toThrow, NullLogger<ExceptionHandlingMiddleware>.Instance);
        
        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        var body = await JsonSerializer.DeserializeAsync<ErrorResponse>(
            context.Response.Body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        
        var requestId = context.TraceIdentifier;
        Assert.False(string.IsNullOrEmpty(body!.RequestId));
        
        return (context.Response.StatusCode, body!, requestId);
    }
    
    [Fact]
    public async Task NotFoundException_Returns404_WithMessage()
    {
        var (status, body, requestId) = await RunAsync(new NotFoundException("Household", 123));

        Assert.Equal(404, body.StatusCode);
        Assert.Equal(404, status);
        Assert.Equal(requestId, body.RequestId);
        Assert.Contains("Household", body.Message);
    }
    
    [Fact]
    public async Task ForbiddenException_Returns403_WithMessage()
    {
        var (status, body, requestId) = await RunAsync(new ForbiddenException("Add Household", 123));

        Assert.Equal(403, status);
        Assert.Equal(403, body.StatusCode);
        Assert.Equal(requestId, body.RequestId);
        Assert.Contains("Add Household", body.Message);
    }
    
    [Fact]
    public async Task ValidationException_Returns400_WithMessage()
    {
        var (status, body, requestId) = await RunAsync(new ValidationException("HouseholdName", "Names must be unique") );

        Assert.Equal(400, status);
        Assert.Equal(400, body.StatusCode);
        Assert.Equal(requestId, body.RequestId);
        Assert.Contains("HouseholdName", body.Message);
    } 
    
    [Fact]
    public async Task ConcurrencyConflictException_Returns409_WithMessage()
    {
        var (status, body, requestId) = await RunAsync(new ConcurrencyConflictException("Remove Household", 123));

        Assert.Equal(409, status);
        Assert.Equal(409, body.StatusCode);
        Assert.Equal(requestId, body.RequestId);
        Assert.Contains("Remove Household", body.Message);
    }
    
    [Fact]
    public async Task UnhandledException_Returns500_WithGenericMessage()
    {
        var (status, body, requestId) = await RunAsync(new InvalidOperationException("Ka-Boom"));

        Assert.Equal(500, status);
        Assert.Equal("An unexpected error occurred.", body.Message);
        Assert.DoesNotContain("Ka-Boom", body.Message);
        Assert.Equal(requestId, body.RequestId);
    }
}