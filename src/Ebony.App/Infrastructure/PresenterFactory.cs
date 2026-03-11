using Ebony.Features.Browser.Album;
using Ebony.Features.Shell;
using Ebony.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Ebony.App.Infrastructure;

public class PresenterFactory(IServiceProvider serviceProvider) : IPresenterFactory
{
    public TPresenter Create<TPresenter>() where TPresenter : IPresenter
    {
        return serviceProvider.GetRequiredService<TPresenter>();
    }
}