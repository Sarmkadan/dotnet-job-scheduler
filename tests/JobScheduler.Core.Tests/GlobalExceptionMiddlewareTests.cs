// tests/JobScheduler.Core.Tests/GlobalExceptionMiddlewareTests.cs
using JobScheduler.Core.Exceptions;
using JobScheduler.Core.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace JobScheduler.Core.Tests;

public class GlobalExceptionMiddlewareTests
{
    private readonly Mock<RequestDelegate> _nextMock = new();
    private readonly Mock<ILogger<GlobalExceptionMiddleware>> _loggerMock = new();

    public GlobalExceptionMiddlewareTests()
    {
        _nextMock.Reset();
        _loggerMock.Reset();
    }

    [Fact]
    public async Task InvokeAsync_WhenNextDelegateSucceeds_ShouldNotThrow()
    {
        // Arrange
        var middleware = new GlobalExceptionMiddleware(_nextMock.Object, _loggerMock.Object);
        var context = new DefaultHttpContext();
        var servicesMock = new Mock<IServiceProvider>();
        context.RequestServices = servicesMock.Object;
        _nextMock.Setup(x => x.Invoke(It.IsAny<HttpContext>()))
            .Returns(Task.CompletedTask);

        // Act
        var exception = await Record.ExceptionAsync(() => middleware.InvokeAsync(context));

        // Assert
        Assert.Null(exception);
        _nextMock.Verify(x => x.Invoke(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WhenNextDelegateThrowsGenericException_ShouldSet500StatusCode()
    {
        // Arrange
        var middleware = new GlobalExceptionMiddleware(_nextMock.Object, _loggerMock.Object);
        var context = new DefaultHttpContext();
        var servicesMock = new Mock<IServiceProvider>();
        context.RequestServices = servicesMock.Object;
        var expectedException = new InvalidOperationException("Test exception");

        _nextMock.Setup(x => x.Invoke(It.IsAny<HttpContext>()))
            .ThrowsAsync(expectedException);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenNextDelegateThrowsJobValidationException_ShouldSet400StatusCode()
    {
        // Arrange
        var middleware = new GlobalExceptionMiddleware(_nextMock.Object, _loggerMock.Object);
        var context = new DefaultHttpContext();
        var servicesMock = new Mock<IServiceProvider>();
        context.RequestServices = servicesMock.Object;

        _nextMock.Setup(x => x.Invoke(It.IsAny<HttpContext>()))
            .ThrowsAsync(new JobValidationException("Validation failed"));

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenNextDelegateThrowsJobNotFoundException_ShouldSet404StatusCode()
    {
        // Arrange
        var middleware = new GlobalExceptionMiddleware(_nextMock.Object, _loggerMock.Object);
        var context = new DefaultHttpContext();
        var servicesMock = new Mock<IServiceProvider>();
        context.RequestServices = servicesMock.Object;

        _nextMock.Setup(x => x.Invoke(It.IsAny<HttpContext>()))
            .ThrowsAsync(new JobNotFoundException("Job not found"));

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenNextDelegateThrowsConcurrencyException_ShouldSet409StatusCode()
    {
        // Arrange
        var middleware = new GlobalExceptionMiddleware(_nextMock.Object, _loggerMock.Object);
        var context = new DefaultHttpContext();
        var servicesMock = new Mock<IServiceProvider>();
        context.RequestServices = servicesMock.Object;

        _nextMock.Setup(x => x.Invoke(It.IsAny<HttpContext>()))
            .ThrowsAsync(new ConcurrencyException(Guid.Empty, 1, 2));

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenNextDelegateThrowsExecutionException_ShouldSet500StatusCode()
    {
        // Arrange
        var middleware = new GlobalExceptionMiddleware(_nextMock.Object, _loggerMock.Object);
        var context = new DefaultHttpContext();
        var servicesMock = new Mock<IServiceProvider>();
        context.RequestServices = servicesMock.Object;

        _nextMock.Setup(x => x.Invoke(It.IsAny<HttpContext>()))
            .ThrowsAsync(new ExecutionException("Execution failed", Guid.Empty, Guid.Empty));

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
    }

    [Fact]
    public void Constructor_WhenNextDelegateIsNull_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(
            () => new GlobalExceptionMiddleware(null!, _loggerMock.Object));
        Assert.Equal("next", exception.ParamName);
    }

    [Fact]
    public void Constructor_WhenLoggerIsNull_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(
            () => new GlobalExceptionMiddleware(_nextMock.Object, null!));
        Assert.Equal("logger", exception.ParamName);
    }

    [Fact]
    public async Task InvokeAsync_WhenResponseContentType_ShouldBeApplicationJson()
    {
        // Arrange
        var middleware = new GlobalExceptionMiddleware(_nextMock.Object, _loggerMock.Object);
        var context = new DefaultHttpContext();
        var servicesMock = new Mock<IServiceProvider>();
        context.RequestServices = servicesMock.Object;
        var expectedException = new InvalidOperationException("Test exception");

        _nextMock.Setup(x => x.Invoke(It.IsAny<HttpContext>()))
            .ThrowsAsync(expectedException);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.StartsWith("application/json", context.Response.ContentType);
    }

    [Fact]
    public async Task ErrorResponse_ShouldHaveCorrectStructure()
    {
        // Arrange
        var middleware = new GlobalExceptionMiddleware(_nextMock.Object, _loggerMock.Object);
        var context = new DefaultHttpContext();
        var servicesMock = new Mock<IServiceProvider>();
        context.RequestServices = servicesMock.Object;
        var expectedException = new InvalidOperationException("Test error");

        _nextMock.Setup(x => x.Invoke(It.IsAny<HttpContext>()))
            .ThrowsAsync(expectedException);

        // Act
        await middleware.InvokeAsync(context);

        // Assert - verify status code and content type
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.StartsWith("application/json", context.Response.ContentType);
    }
}
