using Aria.Core.Connection;
using Microsoft.Extensions.DependencyInjection;

namespace Aria.Infrastructure.Connection;

public class ScopedBackendConnection(IBackendConnection backendConnection, IServiceScope scope) : IDisposable
{
    public IBackendConnection Connection => backendConnection;

    public void Dispose()
    {
        Connection.Dispose();
        scope.Dispose();
    }
}

public class BaseBackendConnectionFactory<TBackendConnection, TConnectionProfile>(IServiceProvider serviceProvider) : IBackendConnectionFactory
    where TBackendConnection : IBackendConnection
    where TConnectionProfile : IConnectionProfile
{
    public virtual bool CanHandle(IConnectionProfile profile)
    {
        return profile is TConnectionProfile;
    }

    public async Task<ScopedBackendConnection> CreateAsync(IConnectionProfile profile)
    {
        if (profile is not TConnectionProfile connectionProfile) throw new ArgumentException("Profile is not an supported profile");
        
        // We use a scope for this factory, so all its dependencies are scoped to this connection instance
        var scope = serviceProvider.CreateScope();
        try
        {
            // Set the connection context for the scope
            var context = scope.ServiceProvider.GetRequiredService<ConnectionContext>();
            context.Profile = profile;            
            
            var connection = scope.ServiceProvider.GetRequiredService<TBackendConnection>();
            await ConfigureAsync(connection, connectionProfile);
            
            // Consider if I should add the connection to the scope as well
            
            return new ScopedBackendConnection(connection, scope);
        }
        catch
        {
            scope.Dispose();
            throw;
        }
    }

    protected virtual Task ConfigureAsync(TBackendConnection connection, TConnectionProfile profile)
    {
        return Task.CompletedTask; 
    }
}