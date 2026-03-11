using CommunityToolkit.Mvvm.Messaging.Messages;
using JetBrains.Annotations;

namespace Ebony.Core.Queue;

[UsedImplicitly]
public sealed class QueueStateChangedMessage(QueueStateChangedFlags flags) : ValueChangedMessage<QueueStateChangedFlags>(flags);