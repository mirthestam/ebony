namespace Ebony.Features.Shell;

public interface IPresenter
{
    
}

public interface IRootPresenter<T> : IPresenter
{
    void Attach(T view, AttachContext context);
    T? View { get; }
}

public interface IPresenter<T> : IPresenter
{
    void Attach(T view);
    T? View { get; }
}