using Ebony.Infrastructure.Connection;

namespace Ebony.Backends.MPD.Connection;

public class BackendConnectionFactory(IServiceProvider serviceProvider) : BaseBackendConnectionFactory<BackendConnection, ConnectionProfile>(serviceProvider)
{
    protected override Task ConfigureAsync(BackendConnection connection, ConnectionProfile profile)
    {
        var config = new ConnectionConfig(profile.Socket, profile.UseSocket, profile.Host, profile.Port, profile.Password);
        connection.SetCredentials(config);
        return Task.CompletedTask;
    }
}