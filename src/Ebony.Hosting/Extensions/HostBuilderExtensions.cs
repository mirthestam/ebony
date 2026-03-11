using Microsoft.Extensions.Hosting;

namespace Ebony.Hosting.Extensions;

public static class HostBuilderExtensions
{
    extension(IHostBuilder hostBuilder)
    {
        public IHostBuilder UseGtk(Action<IGtkBuilder> configureDelegate = null)
        {
            return GtkUtility.UseGtk(hostBuilder, configureDelegate);
        }
    }
}