using System.ComponentModel;
using System.Runtime.CompilerServices;
using Aria.Core.Queue;
using GObject;
using Object = GObject.Object;

namespace Aria.Features.Player.Queue;

[Subclass<Object>]
public partial class QueueModel : INotifyPropertyChanged
{
    public QueueMode Mode
    {
        get;
        set
        {
            if (field == value) return;


            field = value;
            OnPropertyChanged();
        }
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }    
}