using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Ebony.Hosting;

public class GtkHostedService(ILogger<GtkHostedService> logger, GtkThread thread, IGtkContext context) : IHostedService
{
    private readonly ILogger<GtkHostedService> _logger = logger;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return Task.CompletedTask;

        // Make the UI thread go
        thread.Start();
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (context.IsRunning)
            // Foce Stop application
            context.Application.Quit();
        await Task.CompletedTask;
    }
}