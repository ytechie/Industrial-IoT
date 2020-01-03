// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Services {
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.Utils;
    using Opc.Ua;
    using Opc.Ua.Client;
    using Opc.Ua.Extensions;
    using Serilog;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Subscription services implementation
    /// </summary>
    public class SubscriptionManager : ISubscriptionManager, IDisposable {

        /// <inheritdoc/>
        public int TotalSubscriptionCount => _subscriptions.Count;

        /// <summary>
        /// Create subscription manager
        /// </summary>
        /// <param name="client"></param>
        /// <param name="codec"></param>
        /// <param name="logger"></param>
        public SubscriptionManager(IEndpointServices client, IVariantEncoderFactory codec,
            ILogger logger) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _codec = codec ?? throw new ArgumentNullException(nameof(codec));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public Task<ISubscription> GetOrCreateSubscriptionAsync(SubscriptionModel subscriptionModel) {
            if (string.IsNullOrEmpty(subscriptionModel?.Id)) {
                throw new ArgumentNullException(nameof(subscriptionModel));
            }
            var sub = _subscriptions.GetOrAdd(subscriptionModel.Id,
                key => new SubscriptionState(this, subscriptionModel, _logger));
            return Task.FromResult<ISubscription>(sub);
        }

        /// <inheritdoc/>
        public void Dispose() {
            // Cleanup remaining subscriptions
            var subscriptions = _subscriptions.Values.ToList();
            _subscriptions.Clear();
            subscriptions.ForEach(s => Try.Op(() => s.Dispose()));
        }


        // TODO : Timer to lazily invalidate subscriptions after a while




        /// <inheritdoc/>
        public event EventHandler<SubscriptionNotificationModel> OnSubscriptionChange;

        /// <inheritdoc/>
        public event EventHandler<SubscriptionNotificationModel> OnMonitoredItemChange;




        /// <summary>
        /// Synchronize monitored items and triggering configuration in subscription
        /// </summary>
        /// <param name="desiredItems"></param>
        /// <param name="session"></param>
        /// <returns></returns>
        public async Task ApplyAsync(SessionClient session,
            IEnumerable<SubscriptionModel> desiredItems) {

            var update = new HashSet<SubscriptionState>();
            var add = new HashSet<SubscriptionState>();
            if (desiredItems == null) {
                return;
            }

            // Synchronize the desired items with the state of the raw subscription
            var desiredState = desiredItems
                .Select(m => new SubscriptionState(this, m.Id, m.Configuration, _logger))
                .ToHashSetSafe();
            var currentState = _currentState;

            // Remove monitored items not in desired state
            var deletes = currentState.Except(desiredState).ToHashSetSafe();
            await session.DeleteSubscriptionsAsync(null, new UInt32Collection(deletes
                .Where(s => s.ServerId != null).Select(s => s.ServerId.Value)),

            var applyChanges = deletes.Count != 0;

            // Add new monitored items not in current state
            foreach (var toAdd in desiredState.Except(currentState)) {
                _logger.Debug("Adding new monitored item '{item}'...", toAdd);
                add.Add(toAdd);
                applyChanges = true;
            }

            // Update monitored items that have changed
            var desiredUpdates = desiredState.Intersect(currentState)
                .ToDictionary(k => k, v => v);
            foreach (var toUpdate in currentState.Intersect(desiredState)) {
                if (!toUpdate.IsEqualConfiguration(desiredUpdates[toUpdate])) {
                    _logger.Debug("Updating monitored item '{item}'...", toUpdate);
                    update.Add(toUpdate);
                    applyChanges = true;
                }
            }
            return applyChanges;
        }


        /// <summary>
        /// Subscription implementation
        /// </summary>
        internal sealed class SubscriptionState {

            /// <summary>
            /// Server id
            /// </summary>
            public uint? ServerId { get; set; }

            /// <inheritdoc/>
            public string Id { get; }

            /// <inheritdoc/>
            public SubscriptionConfigurationModel Subscription { get; }




            /// <summary>
            /// Subscription wrapper
            /// </summary>
            /// <param name="outer"></param>
            /// <param name="subscription"></param>
            /// <param name="logger"></param>
            public SubscriptionState(SubscriptionManager outer, string id,
                SubscriptionConfigurationModel subscription, ILogger logger) {
                Subscription = subscription.Clone() ??
                    throw new ArgumentNullException(nameof(subscription));
                Id = id,
                _outer = outer ??
                    throw new ArgumentNullException(nameof(outer));
                _logger = logger?.ForContext<SubscriptionState>() ??
                    throw new ArgumentNullException(nameof(logger));
            }


            //    /// <inheritdoc/>
            //    public async Task<SubscriptionNotificationModel> GetSnapshotAsync() {
            //        await _lock.WaitAsync();
            //        try {
            //            var session = _handle.Session;
            //            if (session == null) {
            //                return null;
            //            }
            //
            //            // Get subscription in session
            //            var subscription = session.Subscriptions
            //                .SingleOrDefault(s => s.Handle == this);
            //            if (subscription == null) {
            //                return null;
            //            }
            //
            //            return new SubscriptionNotificationModel {
            //                ServiceMessageContext = session.MessageContext,
            //                ApplicationUri = session.Endpoint.Server.ApplicationUri,
            //                EndpointUrl = session.Endpoint.EndpointUrl,
            //                SubscriptionId = Id,
            //                Notifications = subscription.MonitoredItems
            //                    .Select(m => m.LastValue.ToMonitoredItemNotification(m))
            //                    .Where(m => m != null)
            //                    .ToList()
            //            };
            //        }
            //        finally {
            //            _lock.Release();
            //        }
            //    }


            /// <inheritdoc/>
            public override bool Equals(object obj) {
                if (!(obj is SubscriptionState item)) {
                    return false;
                }
                if (Id != item.Id) {
                    return false;
                }
                return true;
            }

            /// <inheritdoc/>
            public override int GetHashCode() {
                var hashCode = 1301977042;
                hashCode = (hashCode * -1521134295) +
                    EqualityComparer<string>.Default.GetHashCode(Id);
                return hashCode;
            }

            /// <inheritdoc/>
            public override string ToString() {
                return $"Subscription {Id ?? "<unknown>"}";
            }

            /// <summary>
            /// Test full equality across all configuration settings
            /// </summary>
            /// <param name="obj"></param>
            /// <returns></returns>
            public bool IsEqualConfiguration(object obj) {
                if (!Equals(obj)) {
                    return false;
                }
                var item = obj as SubscriptionState;
                if (Subscription.KeepAliveCount != item.Subscription.KeepAliveCount) {
                    return false;
                }
                if (Subscription.LifetimeCount != item.Subscription.LifetimeCount) {
                    return false;
                }
                if (Subscription.MaxNotificationsPerPublish != item.Subscription.MaxNotificationsPerPublish) {
                    return false;
                }
                if (Subscription.Priority != item.Subscription.Priority) {
                    return false;
                }
                if (Subscription.PublishingInterval != item.Subscription.PublishingInterval) {
                    return false;
                }
                return true;
            }





            /// <summary>
            /// Retriev a raw subscription with all settings applied (no lock)
            /// </summary>
            /// <param name="session"></param>
            /// <param name="configuration"></param>
            /// <param name="monitoredItems"></param>
            /// <returns></returns>
            private async Task<Subscription> ApplyAsync(Session session,
                SubscriptionConfigurationModel configuration,
                IEnumerable<MonitoredItemModel> monitoredItems) {

                var subscription = session.Subscriptions.SingleOrDefault(s => s.Handle == this);
                if (subscription == null) {

                    if (configuration != null) {
                        // Apply new configuration right here saving us from modifying later
                        Subscription = configuration.Clone();
                    }

                    subscription = new Subscription(session.DefaultSubscription) {
                        Handle = this,
                        PublishingInterval = (int)
                            (Subscription.PublishingInterval ?? TimeSpan.Zero).TotalMilliseconds,
                        DisplayName = Id,
                        KeepAliveCount = Subscription.KeepAliveCount ?? 10,
                        MaxNotificationsPerPublish = Subscription.MaxNotificationsPerPublish ?? 0,
                        PublishingEnabled = true,
                        Priority = Subscription.Priority ?? 0,
                        LifetimeCount = Subscription.LifetimeCount ?? 2400,
                        TimestampsToReturn = TimestampsToReturn.Both,
                        FastDataChangeCallback = OnSubscriptionDataChanged

                        // MaxMessageCount = 10,
                    };

                    session.AddSubscription(subscription);
                    subscription.Create();

                    _logger.Debug("Added subscription '{name}' to session '{session}'.",
                         Id, session.SessionName);
                }
                else {
                    // Set configuration on original subscription
                    ReviseConfiguration(subscription, configuration);
                }
                // Set currently monitored
                await SetMonitoredItemsAsync(subscription, monitoredItems);
                return subscription;
            }

            /// <summary>
            /// Synchronize monitored items and triggering configuration in subscription
            /// </summary>
            /// <param name="desiredItems"></param>
            /// <param name="currentItems"></param>
            /// <param name="deletes"></param>
            /// <param name="add"></param>
            /// <param name="update"></param>
            /// <returns></returns>
            public bool GetMonitoredItemChangesPhase1(
                IEnumerable<MonitoredItemModel> desiredItems,
                IEnumerable<MonitoredItemState> currentItems,
                out HashSet<MonitoredItemState> deletes,
                out HashSet<MonitoredItemState> add,
                out HashSet<MonitoredItemState> update) {

                update = new HashSet<MonitoredItemState>();
                add = new HashSet<MonitoredItemState>();
                if (desiredItems == null) {
                    deletes = new HashSet<MonitoredItemState>();
                    return false;
                }

                // Synchronize the desired items with the state of the raw subscription
                var desiredState = desiredItems
                    .Select(m => new MonitoredItemState(m, _logger))
                    .ToHashSetSafe();
                var currentState = currentItems
                    .ToHashSetSafe();

                // Remove monitored items not in desired state
                deletes = currentState.Except(desiredState).ToHashSetSafe();
                var applyChanges = deletes.Count != 0;

                //  // Re-associate detached handles
                //  foreach (var detached in rawSubscription.MonitoredItems
                //      .Where(m => m.Handle == null)) {
                //
                //      // TODO: Claim monitored item
                //
                //      rawSubscription.RemoveItem(detached);
                //  }

                // Add new monitored items not in current state
                foreach (var toAdd in desiredState.Except(currentState)) {
                    _logger.Debug("Adding new monitored item '{item}'...", toAdd);
                    add.Add(toAdd);
                    applyChanges = true;
                }

                // Update monitored items that have changed
                var desiredUpdates = desiredState.Intersect(currentState)
                    .ToDictionary(k => k, v => v);
                foreach (var toUpdate in currentState.Intersect(desiredState)) {
                    if (!toUpdate.IsEqualConfiguration(desiredUpdates[toUpdate])) {
                        _logger.Debug("Updating monitored item '{item}'...", toUpdate);
                        update.Add(toUpdate);
                        applyChanges = true;
                    }
                }
                return applyChanges;
            }

            /// <summary>
            /// Get trigger links from all items
            /// </summary>
            /// <param name="currentItems"></param>
            /// <param name="added"></param>
            /// <param name="removed"></param>
            /// <param name="modeChanges"></param>
            /// <returns></returns>
            public void GetMonitoredItemChangesPhase2(IEnumerable<MonitoredItemState> currentItems,
                out HashSet<uint> added, out HashSet<uint> removed,
                out Dictionary<MonitoringItemMode, List<uint>> modeChanges) {

                added = new HashSet<uint>();
                removed = new HashSet<uint>();
                modeChanges = new Dictionary<MonitoringItemMode, List<uint>>();

                var map = currentItems.ToDictionary(
                    k => k.MonitoredItem.Id ?? k.MonitoredItem.StartNodeId, v => v);
                foreach (var item in currentItems.ToList()) {
                    if (item.MonitoredItem.TriggerId != null &&
                        map.TryGetValue(item.MonitoredItem.TriggerId, out var trigger)) {
                        trigger.AddTriggerLink(item.Status.ServerId);
                    }
                }

                // Set up any new trigger configuration if needed
                foreach (var item in currentItems.ToList()) {
                    item.GetTriggeringLinks(ref added, ref removed);
                }

                // Change monitoring mode of all items if necessary
                foreach (var change in currentItems.GroupBy(i => i.GetMonitoringModeChange())) {
                    if (change.Key == null) {
                        continue;
                    }
                    modeChanges.Add(change.Key.Value,
                        change.Select(i => i.Status.ServerId ?? 0).ToList());
                }
            }

            /// <summary>
            /// Subscription data changed
            /// </summary>
            /// <param name="subscription"></param>
            /// <param name="notification"></param>
            /// <param name="stringTable"></param>
            private void OnSubscriptionDataChanged(Subscription subscription,
                DataChangeNotification notification, IList<string> stringTable) {
                try {
                    if (OnSubscriptionChange == null) {
                        return;
                    }
                    var message = new SubscriptionNotificationModel {
                        ServiceMessageContext = subscription.Session.MessageContext,
                        ApplicationUri = subscription.Session.Endpoint.Server.ApplicationUri,
                        EndpointUrl = subscription.Session.Endpoint.EndpointUrl,
                        SubscriptionId = Id,
                        Notifications = notification
                            .ToMonitoredItemNotifications(subscription.MonitoredItems)
                            .ToList()
                    };
                    OnSubscriptionChange?.Invoke(this, message);
                }
                catch (Exception ex) {
                    _logger.Debug(ex, "Exception processing subscription notification");
                }
            }

            /// <summary>
            /// Monitored item notification handler
            /// </summary>
            /// <param name="monitoredItem"></param>
            /// <param name="e"></param>
            private void OnMonitoredItemChanged(MonitoredItem monitoredItem,
                MonitoredItemNotificationEventArgs e) {
                try {
                    if (OnMonitoredItemChange == null) {
                        return;
                    }
                    if (e?.NotificationValue == null || monitoredItem?.Subscription?.Session == null) {
                        return;
                    }
                    if (!(e.NotificationValue is MonitoredItemNotification notification)) {
                        return;
                    }
                    if (!(notification.Value is DataValue value)) {
                        return;
                    }

                    var message = new SubscriptionNotificationModel {
                        ServiceMessageContext = monitoredItem.Subscription.Session.MessageContext,
                        ApplicationUri = monitoredItem.Subscription.Session.Endpoint.Server.ApplicationUri,
                        EndpointUrl = monitoredItem.Subscription.Session.Endpoint.EndpointUrl,
                        SubscriptionId = Id,
                        Notifications = new List<MonitoredItemNotificationModel> {
                            notification.ToMonitoredItemNotification(monitoredItem)
                        }
                    };
                    OnMonitoredItemChange(this, message);
                }
                catch (Exception ex) {
                    _logger.Debug(ex, "Exception processing monitored item notification");
                }
            }

            /// <summary>
            /// Monitored item diffing engine
            /// </summary>
            public class MonitoredItemState {

                /// <summary>
                /// Client Id
                /// </summary>
                public string ClientId => MonitoredItem.Id;

                /// <summary>
                /// Server id
                /// </summary>
                public uint? ServerId => Status.ServerId;

                /// <summary>
                /// Status
                /// </summary>
                public MonitoredItemStatusModel Status { get; private set; }

                /// <summary>
                /// Monitored item model
                /// </summary>
                public MonitoredItemModel MonitoredItem { get; set; }

                /// <summary>
                /// Create wrapper
                /// </summary>
                /// <param name="monitoredItem"></param>
                /// <param name="logger"></param>
                public MonitoredItemState(MonitoredItemModel monitoredItem, ILogger logger) {
                    _logger = logger?.ForContext<MonitoredItemState>() ??
                        throw new ArgumentNullException(nameof(logger));
                    MonitoredItem = monitoredItem.Clone() ??
                        throw new ArgumentNullException(nameof(monitoredItem));
                }

                /// <inheritdoc/>
                public override bool Equals(object obj) {
                    if (!(obj is MonitoredItemState item)) {
                        return false;
                    }
                    if (MonitoredItem.Id != item.MonitoredItem.Id) {
                        return false;
                    }
                    if (!MonitoredItem.RelativePath.SequenceEqualsSafe(item.MonitoredItem.RelativePath)) {
                        return false;
                    }
                    if (MonitoredItem.StartNodeId != item.MonitoredItem.StartNodeId) {
                        return false;
                    }
                    if (MonitoredItem.IndexRange != item.MonitoredItem.IndexRange) {
                        return false;
                    }
                    if (MonitoredItem.AttributeId != item.MonitoredItem.AttributeId) {
                        return false;
                    }
                    return true;
                }

                /// <inheritdoc/>
                public override int GetHashCode() {
                    var hashCode = 1301977042;
                    hashCode = (hashCode * -1521134295) +
                        EqualityComparer<string>.Default.GetHashCode(MonitoredItem.Id);
                    hashCode = (hashCode * -1521134295) +
                        EqualityComparer<string[]>.Default.GetHashCode(MonitoredItem.RelativePath);
                    hashCode = (hashCode * -1521134295) +
                        EqualityComparer<string>.Default.GetHashCode(MonitoredItem.StartNodeId);
                    hashCode = (hashCode * -1521134295) +
                        EqualityComparer<string>.Default.GetHashCode(MonitoredItem.IndexRange);
                    hashCode = (hashCode * -1521134295) +
                        EqualityComparer<NodeAttribute?>.Default.GetHashCode(MonitoredItem.AttributeId);
                    return hashCode;
                }

                /// <inheritdoc/>
                public override string ToString() {
                    return $"Item {MonitoredItem.Id ?? "<unknown>"}{Status.ServerId}: " +
                        $"'{MonitoredItem.StartNodeId}'" +
                        $" - {(Status.ServerId != null ? "" : "not ")}created";
                }

                /// <summary>
                /// Add the monitored item identifier of the triggering item.
                /// </summary>
                /// <param name="id"></param>
                internal void AddTriggerLink(uint? id) {
                    if (id != null) {
                        _newTriggers.Add(id.Value);
                    }
                }

                /// <summary>
                /// Get triggering configuration changes for this item
                /// </summary>
                /// <param name="addLinks"></param>
                /// <param name="removeLinks"></param>
                /// <returns></returns>
                internal bool GetTriggeringLinks(ref HashSet<uint> addLinks,
                    ref HashSet<uint> removeLinks) {
                    var remove = _triggers.Except(_newTriggers).ToList();
                    var add = _newTriggers.Except(_triggers).ToList();
                    _triggers = _newTriggers;
                    _newTriggers = new HashSet<uint>();
                    foreach (var item in add) addLinks.Add(item);
                    foreach (var item in remove) removeLinks.Add(item);
                    if (add.Count > 0 || remove.Count > 0) {
                        _logger.Debug("{item}: Adding {add} links and removing {remove} links.",
                            this, add.Count, remove.Count);
                        return true;
                    }
                    return false;
                }

                /// <summary>
                /// Get any changes in the monitoring mode
                /// </summary>
                /// <returns></returns>
                internal MonitoringItemMode? GetMonitoringModeChange() {
                    var change = _modeChange;
                    _modeChange = null;
                    return MonitoredItem.MonitoringMode == change ? null : change;
                }

                /// <summary>
                /// Test full equality across all configuration settings
                /// </summary>
                /// <param name="obj"></param>
                /// <returns></returns>
                public bool IsEqualConfiguration(object obj) {
                    if (!Equals(obj)) {
                        return false;
                    }
                    var item = obj as MonitoredItemState;
                    if (MonitoredItem.DiscardNew != item.MonitoredItem.DiscardNew) {
                        return false;
                    }
                    if (!MonitoredItem.AggregateFilter.IsSameAs(item.MonitoredItem.AggregateFilter)) {
                        return false;
                    }
                    if (!MonitoredItem.EventFilter.IsSameAs(item.MonitoredItem.EventFilter)) {
                        return false;
                    }
                    if (!MonitoredItem.DataChangeFilter.IsSameAs(item.MonitoredItem.DataChangeFilter)) {
                        return false;
                    }
                    if (MonitoredItem.MonitoringMode != item.MonitoredItem.MonitoringMode) {
                        return false;
                    }
                    if (MonitoredItem.QueueSize != item.MonitoredItem.QueueSize) {
                        return false;
                    }
                    if (MonitoredItem.SamplingInterval != item.MonitoredItem.SamplingInterval) {
                        return false;
                    }
                    return true;
                }

                private HashSet<uint> _newTriggers = new HashSet<uint>();
                private HashSet<uint> _triggers = new HashSet<uint>();
                private MonitoringItemMode? _modeChange;
                private readonly ILogger _logger;
            }

            private readonly SubscriptionManager _outer;
            private readonly ILogger _logger;
            private readonly SemaphoreSlim _lock;
            private readonly Timer _timer;
            private List<MonitoredItemState> _currentlyMonitored;
        }

        private readonly HashSet<SubscriptionState> _currentState = new HashSet<SubscriptionState>();
        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<string, SubscriptionState> _subscriptions =
            new ConcurrentDictionary<string, SubscriptionState>();
        private readonly IEndpointServices _client;
        private readonly IVariantEncoderFactory _codec;
    }
}