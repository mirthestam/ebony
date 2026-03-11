using Ebony.Features.Shell;

namespace Ebony.Infrastructure;

public interface IPresenterFactory
{
    TPresenter Create<TPresenter>() where TPresenter : IPresenter;
} 