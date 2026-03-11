using System.Net;
using System.Runtime.CompilerServices;
using CodeProject.ObjectPool;
using Microsoft.Extensions.Logging;
using MpcNET;
using MpcNET.Commands.Reflection;
using MpcNET.Commands.Status;
using Timer = System.Timers.Timer;

namespace Ebony.Backends.MPD.Connection;

/// <summary>
///     Manages a MPD server session using multiple connections:
///     1. Status Connection: Dedicated connection for periodic status polling
///     2. Idle Connection: Dedicated connection using MPD's IDLE command for event-driven updates
///     3. Command Pool: Pool of connections available for executing MPD commands from the application
///     This architecture ensures status updates don't block command execution and provides both
///     polling-based and event-driven status updates for optimal responsiveness.
/// </summary>
public sealed class Client(ILoggerFactory loggerFactory)
{
    public static  string Escape(string command)
    {
        return command.Replace("'", "\\'").Replace("\"", "\\\"");
    }
    
    private const int ConnectionPoolSize = 5;
    
    private CancellationTokenSource _cancelIdle = new();
    
    /// <summary>
    ///     A pool of connections available for the application to send commands to MPD.
    ///     Using a pool prevents blocking on the status and idle connections and allows concurrent command execution.
    /// </summary>
    private ObjectPool<PooledObjectWrapper<MpcConnection>>? _connectionPool;

    /// <summary>
    ///     A dedicated connection for MPD that uses the IDLE mechanism.
    ///     We will wait for MPD to return from the IDLE command, which in turn
    ///     tells us something has changed in the server state (e.g., track changed, playlist modified).
    ///     This provides event-driven updates complementing the periodic polling of _statusConnection.
    /// </summary>
    private MpcConnection? _idleConnection;

    private bool _isUpdatingStatus;

    private IPEndPoint? _mpdEndpoint;

    private Timer? _statusUpdater;

    private MpcConnection? StatusConnection { get; set; }

    public bool IsConnected { get; private set; }

    public bool IsConnecting { get; private set; }

    public ConnectionConfig? Config { get; set; }

    public event EventHandler? ConnectionChanged;

    public event EventHandler<IdleResponseEventArgs>? IdleResponseReceived;

    public event EventHandler<StatusChangedEventArgs>? StatusChanged;

    public async Task DisconnectAsync()
    {
        await Disconnect().ConfigureAwait(false);
    }    
    
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        IsConnecting = true;
        await Disconnect().ConfigureAwait(false);
        
        try
        {
            ConnectionChanged?.Invoke(this, EventArgs.Empty);
            await Connect(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            IsConnecting = false;
            ConnectionChanged?.Invoke(this, EventArgs.Empty);
        }

        IsConnecting = false;
    }
    
    public async Task<ConnectionScope> CreateConnectionScopeAsync([CallerMemberName]string operation = "", CancellationToken token = default)
    {
        if (_connectionPool == null) throw new InvalidOperationException("Connection pool not initialized");
        
        var wrapper = await _connectionPool.GetObjectAsync(token).ConfigureAwait(false);
        var scopeLogger = loggerFactory.CreateLogger<ConnectionScope>();        
        return new ConnectionScope(wrapper, scopeLogger, operation);
    }
    
    /// <summary>
    ///     Sends a command to MPD using a connection from the command pool.
    ///     This method retrieves an available connection from _connectionPool, sends the command,
    ///     and automatically returns the connection to the pool when done.
    /// </summary>
    public async Task<CommandResult<T>> SendCommandAsync<T>(IMpcCommand<T> command,  [CallerMemberName]string operation = "", CancellationToken token = default)
    {
        using var scope = await CreateConnectionScopeAsync(operation, token).ConfigureAwait(false);
        return await scope.SendCommandAsync(command).ConfigureAwait(false);        
    }

    private MpcConnection? GetConnection(ConnectionType connectionType)
    {
        var connection = connectionType switch
        {
            ConnectionType.Idle => _idleConnection,
            ConnectionType.Status => StatusConnection,
            ConnectionType.Pool => throw new InvalidOperationException("Pool connection not supported"),
            _ => throw new ArgumentOutOfRangeException(nameof(connectionType), connectionType, null)
        };

        return connection;
    }

    public async Task UpdateStatusAsync(ConnectionType connectionType)
    {
        var connection = GetConnection(connectionType);
        if (connection == null) throw new InvalidOperationException("connection not initialized");

        if (_isUpdatingStatus)
            // Already updating status
            return;

        _isUpdatingStatus = true;
        try
        {
            var response = await connection.SendAsync(new StatusCommand()).ConfigureAwait(false);

            if (response is { IsResponseValid: true })
                StatusChanged?.Invoke(this, new StatusChangedEventArgs(response.Response.Content));
            else
                throw new InvalidOperationException("Invalid response");
        }
        catch
        {
            // Something went wrong. Let's reconnect
            // TODO: This layer needs proper connection recovery 
        }
        finally
        {
            _isUpdatingStatus = false;
        }
    }

    private async Task Connect(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return;

        if (Config == null) throw new InvalidOperationException("Config not set");

        if (!Config.UseSocket)
        {
            if (!IPAddress.TryParse(Config.Host, out var ipAddress))
                // TODO: Try to fetch IP address from DNS
                throw new InvalidOperationException("Invalid host");
            
            _mpdEndpoint = new IPEndPoint(ipAddress, Config.Port);            
        }
        
        StatusConnection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        // TODO: Check version here. Abort is the server version is too old
        
        _connectionPool = new ObjectPool<PooledObjectWrapper<MpcConnection>>(ConnectionPoolSize,
            async poolToken =>
            {
                var c = await OpenConnectionAsync(poolToken).ConfigureAwait(false);
                return new PooledObjectWrapper<MpcConnection>(c)
                {
                    // Check our internal global IsConnected status
                    OnValidateObject = _ => IsConnected && !cancellationToken.IsCancellationRequested,
                    OnReleaseResources = conn => conn?.Dispose()
                };
            }
        );

        _idleConnection = await OpenConnectionAsync(_cancelIdle.Token).ConfigureAwait(false);

        InitializeStatusUpdater(_cancelIdle.Token);

        IsConnecting = false;
        IsConnected = true;

        ConnectionChanged?.Invoke(this, EventArgs.Empty);
    }

    private async Task Disconnect()
    {
        if (IsConnected)
        {
            IsConnected = false;
            ConnectionChanged?.Invoke(this, EventArgs.Empty);
        }

        // Stop the IDLE connection
        await _cancelIdle.CancelAsync().ConfigureAwait(false);
        _cancelIdle = new CancellationTokenSource();

        // Stop the status updater
        _statusUpdater?.Stop();
        _statusUpdater?.Dispose();
        if (StatusConnection != null) await StatusConnection.DisconnectAsync().ConfigureAwait(false);
        
        // Clear the connection pool.
        // This will implicitly close inner connections
        _connectionPool?.Clear();

        _idleConnection = null;
        StatusConnection = null;
    }

    private void InitializeStatusUpdater(CancellationToken cancellationToken = default)
    {
        async Task? StatusLoop()
        {
            while (true)
            {
                if (_statusUpdater?.Enabled != true && StatusConnection is { IsConnected: true })
                {
                    // Update the status every second.
                    _statusUpdater?.Stop();
                    _statusUpdater = new Timer(1000);
                    _statusUpdater.Elapsed += async (s, e) => await UpdateStatusAsync(ConnectionType.Status).ConfigureAwait(false);
                    _statusUpdater.Start();
                }

                try
                {
                    // Run the idleConnection in a wrapper task since MpcNET isn't fully async and will block here
                    if (_idleConnection is null) throw new InvalidOperationException("Idle connection not initialized");
    
                    var idleChangesTask = Task.Run(async () =>
                        await _idleConnection.SendAsync(
                            new IdleCommand("stored_playlist playlist player mixer output options update")), cancellationToken);
                    
                    await Task.WhenAny(idleChangesTask, Task.Delay(-1, cancellationToken)).ConfigureAwait(false);

                    if (cancellationToken.IsCancellationRequested || _idleConnection is not { IsConnected: true })
                    {
                        // Disconnect the idle connection
                        _idleConnection?.DisconnectAsync();
                        break;
                    }

                    // Process the result of the message  from the IDLE connection
                    var message = idleChangesTask.Result;

                    if (message.IsResponseValid)
                        HandleIdleResponseAsync(message.Response.Content);
                    else
                        throw new Exception(message.Response?.Content);
                }
                catch
                {
                    if (cancellationToken.IsCancellationRequested) break;
                    IsConnected = false;
                    ConnectionChanged?.Invoke(this, EventArgs.Empty);
                    // TODO Another point  to do connection  recovery.
                    throw;
                }
            }
        }

        Task.Run(StatusLoop, cancellationToken).ConfigureAwait(false);
    }

    private void HandleIdleResponseAsync(string responseContent)
    {
        IdleResponseReceived?.Invoke(this, new IdleResponseEventArgs(responseContent));
    }


    private async Task<MpcConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new MpcConnection(_mpdEndpoint);
        await connection.ConnectAsync(cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(Config?.Password)) return connection;

        var response = await connection.SendAsync(new PasswordCommand(Config.Password)).ConfigureAwait(false);
        return !response.IsResponseValid
            ? throw new InvalidOperationException("Invalid password")
            : connection;
    }
}