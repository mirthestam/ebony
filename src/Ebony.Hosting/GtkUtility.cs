using System.Reflection;
using Gtk;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Ebony.Hosting;

public static class GtkUtility
{
    private const string GtkContextKey = "GtkContext";

    internal static IHostBuilder UseGtk(IHostBuilder hostBuilder, Action<IGtkBuilder> configureDelegate)
    {
        return hostBuilder.ConfigureServices((_, serviceCollection) =>
            InnerConfigureGtk(hostBuilder.Properties, serviceCollection, configureDelegate));
    }

    private static bool TryRetrieveGtkContext(IDictionary<object, object> properties, out IGtkContext gtkContext)
    {
        if (properties.TryGetValue(GtkContextKey, out var gtkContextAsObject))
        {
            gtkContext = (IGtkContext)gtkContextAsObject;
            return true;
        }

        gtkContext = new GtkContext();
        properties[GtkContextKey] = gtkContext;
        return false;
    }

    private static void InnerConfigureGtk(IDictionary<object, object> properties, IServiceCollection serviceCollection,
        Action<IGtkBuilder>? configureDelegate = null)
    {
        var builder = new GtkBuilder();
        configureDelegate?.Invoke(builder);

        if (!TryRetrieveGtkContext(properties, out var wpfContext))
        {
            serviceCollection.AddSingleton(wpfContext);
            serviceCollection.AddSingleton(serviceProvider => new GtkThread(serviceProvider));
            serviceCollection.AddHostedService<GtkHostedService>();
        }

        switch (builder.GtkApplicationType)
        {
            case GtkApplicationType.Gtk:
                var gtkApplication = Application.New(builder.ApplicationId, builder.ApplicationFlags);
                serviceCollection.AddSingleton(gtkApplication);
                break;

            case GtkApplicationType.Adw:
                var adwApplication = Adw.Application.New(builder.ApplicationId, builder.ApplicationFlags);
                serviceCollection.AddSingleton(adwApplication);
                serviceCollection.AddSingleton<Application>(serviceProvider =>
                {
                    var application = serviceProvider.GetRequiredService<Adw.Application>();

                    application.OnActivate += (sender, args) =>
                    {
                        foreach (var initializer in builder.GTypeInitializers) initializer();
                    };

                    return application;
                });
                break;
            default:
                throw new InvalidOperationException("Unsupported application type");
        }

        // Load all Gio resources from disk
        var rootPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        foreach (var fullResourcePath in builder.ResourcePaths.Select(resourcePath => Path.GetFullPath(Path.Combine(rootPath + "/" + resourcePath))))
        {
            Gio.Functions.ResourcesRegister(Gio.Functions.ResourceLoad(fullResourcePath));
        }

        serviceCollection.AddSingleton(builder.WindowType, serviceProvider =>
        {
            var application = serviceProvider.GetRequiredService<Application>();
            var window = builder.WindowFactory(serviceProvider);
            window.SetApplication(application);
            return window;
        });
        
        var applicationWindowType = typeof(ApplicationWindow);
        if (applicationWindowType.IsAssignableFrom(builder.WindowType))
            serviceCollection.AddSingleton(applicationWindowType,
                serviceProvider => { return serviceProvider.GetRequiredService(builder.WindowType); });
    }
}