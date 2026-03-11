using Gtk;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Application = Gio.Application;

namespace Ebony.Hosting;

public class GtkThread
{
    private readonly IGtkContext _context;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ManualResetEvent _serviceManualResetEvent = new(false);
    private readonly IServiceProvider _serviceProvider;

    public GtkThread(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;

        _context = serviceProvider.GetRequiredService<IGtkContext>();
        _lifetime = serviceProvider.GetRequiredService<IHostApplicationLifetime>();

        var thread = new Thread(UiThreadStart)
        {
            IsBackground = true
        };

        thread.Start();
    }

    public void Start()
    {
        _serviceManualResetEvent.Set();
    }

    private void UiThreadStart()
    {
        var application = _serviceProvider.GetRequiredService<Gtk.Application>();
        application.OnShutdown += ApplicationOnOnShutdown;
        application.OnActivate += ApplicationOnOnActivate;
        _context.Application = application;

        // maybethatonactivateiswhere  weshould register GTypes
        _serviceManualResetEvent.WaitOne();

        _context.IsRunning = true;
        application.RunWithSynchronizationContext([]);
    }

    private void ApplicationOnOnShutdown(Application sender, EventArgs args)
    {
        _context.IsRunning = false;
        if (_lifetime.ApplicationStopped.IsCancellationRequested ||
            _lifetime.ApplicationStopping.IsCancellationRequested) return;
        _lifetime.StopApplication();
    }

    private void ApplicationOnOnActivate(Application sender, EventArgs args)
    {
        var applicationWindow = _serviceProvider.GetRequiredService<ApplicationWindow>();
        applicationWindow.Show();
    }
}