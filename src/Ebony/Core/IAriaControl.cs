using Ebony.Core.Extraction;
using Ebony.Infrastructure;

namespace Ebony.Core;

public interface IEbonyControl
{
    public Task InitializeAsync();

    public Task StartAsync(Guid profileId, CancellationToken cancellationToken = default);
    
    public Task StopAsync();
    
    public Id Parse(string id);    
    
    event EventHandler<EngineStateChangedEventArgs>? StateChanged;
    
    public Task RunInspectionAsync();
}