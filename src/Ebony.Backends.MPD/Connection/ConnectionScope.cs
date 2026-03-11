using CodeProject.ObjectPool;
using Microsoft.Extensions.Logging;
using MpcNET;

namespace Ebony.Backends.MPD.Connection;

public record CommandResult<T>(bool IsSuccess, T? Content);

public sealed partial class ConnectionScope : IDisposable
{
    private readonly PooledObjectWrapper<MpcConnection> _wrapper;
    private readonly ILogger<ConnectionScope> _logger;

    private readonly string? _operation;
    private readonly Guid _scopeId = Guid.NewGuid();

    private bool _isDisposed;

    public ConnectionScope(PooledObjectWrapper<MpcConnection> wrapper, ILogger<ConnectionScope> logger,
        string? operation = null)
    {
        _wrapper = wrapper;
        _logger = logger;
        _operation = operation;

        using (_logger.BeginScope(new Dictionary<string, object?>
               {
                   ["MpdScopeId"] = _scopeId,
                   ["MpdOp"] = _operation
               }))
        {
            LogConnectionScopeCreated();
        }
    }

    public async Task<CommandResult<T>> SendCommandAsync<T>(IMpcCommand<T> command)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, nameof(ConnectionScope));

        using (_logger.BeginScope(new Dictionary<string, object?>
               {
                   ["MpdScopeId"] = _scopeId,
                   ["MpdOp"] = _operation,
                   ["MpdCommand"] = command.GetType().Name
               }))
        {
            LogSendingCommandCommand(command.Serialize());

            var response = await _wrapper.InternalResource.SendAsync(command).ConfigureAwait(false);

            return response is { IsResponseValid: true }
                ? new CommandResult<T>(true, response.Response.Content)
                : new CommandResult<T>(false, default);
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;

        using (_logger.BeginScope(new Dictionary<string, object?>
               {
                   ["MpdScopeId"] = _scopeId,
                   ["MpdOp"] = _operation
               }))
        {
            LogConnectionScopeDisposed();
        }

        _wrapper.Dispose();
        _isDisposed = true;
    }

    [LoggerMessage(LogLevel.Trace, "Connection scope created")]
    partial void LogConnectionScopeCreated();
    
    [LoggerMessage(LogLevel.Information, "Sending: {command}")]
    partial void LogSendingCommandCommand(string command);

    [LoggerMessage(LogLevel.Trace, "Connection scope disposed")]
    partial void LogConnectionScopeDisposed();
}