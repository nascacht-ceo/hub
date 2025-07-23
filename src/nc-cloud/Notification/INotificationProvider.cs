using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nc.Cloud.Notification;

/// <summary>
/// Interface for a notification service that handles distributed notifications across application instances.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Publishes a notification to other app instances via the underlying platform.
    /// The notification should be serializable and represent a cross-server event.
    /// </summary>
    Task PublishAsync<T>(T notification, CancellationToken cancellationToken = default)
        where T : INotification;

    /// <summary>
    /// Begins listening for distributed notifications and relays them into the local handler (typically via MediatR).
    /// This is optional if the service is only used for outbound publishing.
    /// </summary>
    Task SubscribeAsync(Func<INotification, Task> onNotification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true if the implementation is actively listening or publishing.
    /// Useful for diagnostics or multi-mode setups (e.g., local-only fallback).
    /// </summary>
    bool IsEnabled { get; }
}
