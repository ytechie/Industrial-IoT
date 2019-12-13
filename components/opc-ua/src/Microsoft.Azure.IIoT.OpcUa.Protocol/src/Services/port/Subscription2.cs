/* ========================================================================
 * Copyright (c) 2005-2016 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 *
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

namespace Microsoft.Azure.IIoT.OpcUa.Protocol {
    using Opc.Ua;
    using Opc.Ua.Client;
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// A subscription
    /// </summary>
    [DataContract(Namespace = Namespaces.OpcUaXsd)]
    public class Subscription : IDisposable {

        /// <summary>
        /// Creates a empty object.
        /// </summary>
        public Subscription() {
            Initialize();
        }

        /// <summary>
        /// Initializes the subscription from a template.
        /// </summary>
        public Subscription(Subscription template) : this(template, false) {
        }

        /// <summary>
        /// Initializes the subscription from a template.
        /// </summary>
        /// <param name="template">The template.</param>
        /// <param name="copyEventHandlers">if set to <c>true</c> the event handlers are copied.</param>
        public Subscription(Subscription template, bool copyEventHandlers) {
            Initialize();

            if (template != null) {
                var displayName = template.DisplayName;

                if (string.IsNullOrEmpty(displayName)) {
                    displayName = DisplayName;
                }

                // remove any existing numeric suffix.
                var index = displayName.LastIndexOf(' ');

                if (index != -1) {
                    try {
                        displayName = displayName.Substring(0, index);
                    }
                    catch {
                        // not a numeric suffix.
                    }
                }

                DisplayName = Utils.Format("{0} {1}", displayName, Utils.IncrementIdentifier(ref s_globalSubscriptionCounter));
                PublishingInterval = template.PublishingInterval;
                KeepAliveCount = template.KeepAliveCount;
                LifetimeCount = template.LifetimeCount;
                MinLifetimeInterval = template.MinLifetimeInterval;
                MaxNotificationsPerPublish = template.MaxNotificationsPerPublish;
                PublishingEnabled = template.PublishingEnabled;
                Priority = template.Priority;
                TimestampsToReturn = template.TimestampsToReturn;
                _maxMessageCount = template._maxMessageCount;
                DefaultItem = (MonitoredItem)template.DefaultItem.MemberwiseClone();
                DefaultItem = template.DefaultItem;
                Handle = template.Handle;
                _maxMessageCount = template._maxMessageCount;
                DisableMonitoredItemCache = template.DisableMonitoredItemCache;

                if (copyEventHandlers) {
                    _StateChanged = template._StateChanged;
                    _PublishStatusChanged = template._PublishStatusChanged;
                    FastDataChangeCallback = template.FastDataChangeCallback;
                    FastEventCallback = template.FastEventCallback;
                }

                // copy the list of monitored items.
                foreach (var monitoredItem in template.MonitoredItems) {
                    var clone = new MonitoredItem(monitoredItem, copyEventHandlers) {
                        Subscription = this
                    };
                    _monitoredItems.Add(clone.ClientHandle, clone);
                }
            }
        }

        /// <summary>
        /// Sets the private members to default values.
        /// </summary>
        private void Initialize() {
            Id = 0;
            DisplayName = "Subscription";
            PublishingInterval = 0;
            KeepAliveCount = 0;
            LifetimeCount = 0;
            MaxNotificationsPerPublish = 0;
            PublishingEnabled = false;
            TimestampsToReturn = TimestampsToReturn.Both;
            _maxMessageCount = 10;
            _outstandingMessageWorkers = 0;
            _messageCache = new LinkedList<NotificationMessage>();
            _monitoredItems = new SortedDictionary<uint, MonitoredItem>();
            _deletedItems = new List<MonitoredItem>();

            DefaultItem = new MonitoredItem {
                DisplayName = "MonitoredItem",
                SamplingInterval = -1,
                MonitoringMode = MonitoringMode.Reporting,
                QueueSize = 0,
                DiscardOldest = true
            };
        }





        /// <summary>
        /// Frees any unmanaged resources.
        /// </summary>
        public void Dispose() {
            Utils.SilentDispose(_publishTimer);
            _publishTimer = null;
        }

        /// <summary>
        /// Raised to indicate that the state of the subscription has changed.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1009:DeclareEventHandlersCorrectly")]
        public event SubscriptionStateChangedEventHandler StateChanged {
            add { _StateChanged += value; }
            remove { _StateChanged -= value; }
        }

        /// <summary>
        /// Raised to indicate the publishing state for the subscription has stopped or resumed (see PublishingStopped property).
        /// </summary>
        public event EventHandler PublishStatusChanged {
            add {
                lock (_cache) {
                    _PublishStatusChanged += value;
                }
            }

            remove {
                lock (_cache) {
                    _PublishStatusChanged -= value;
                }
            }
        }

        /// <summary>
        /// A display name for the subscription.
        /// </summary>
        [DataMember(Order = 1)]
        public string DisplayName { get; set; }

        /// <summary>
        /// The publishing interval.
        /// </summary>
        [DataMember(Order = 2)]
        public int PublishingInterval { get; set; }

        /// <summary>
        /// The keep alive count.
        /// </summary>
        [DataMember(Order = 3)]
        public uint KeepAliveCount { get; set; }

        /// <summary>
        /// The maximum number of notifications per publish request.
        /// </summary>
        [DataMember(Order = 4)]
        public uint LifetimeCount { get; set; }

        /// <summary>
        /// The maximum number of notifications per publish request.
        /// </summary>
        [DataMember(Order = 5)]
        public uint MaxNotificationsPerPublish { get; set; }

        /// <summary>
        /// Whether publishing is enabled.
        /// </summary>
        [DataMember(Order = 6)]
        public bool PublishingEnabled { get; set; }

        /// <summary>
        /// The priority assigned to subscription.
        /// </summary>
        [DataMember(Order = 7)]
        public byte Priority { get; set; }

        /// <summary>
        /// The timestamps to return with the notification messages.
        /// </summary>
        [DataMember(Order = 8)]
        public TimestampsToReturn TimestampsToReturn { get; set; }

        /// <summary>
        /// The maximum number of messages to keep in the internal cache.
        /// </summary>
        [DataMember(Order = 9)]
        public int MaxMessageCount {
            get {
                lock (_cache) {
                    return _maxMessageCount;
                }
            }

            set {
                lock (_cache) {
                    _maxMessageCount = value;
                }
            }
        }

        /// <summary>
        /// The default monitored item.
        /// </summary>
        [DataMember(Order = 10)]
        public MonitoredItem DefaultItem { get; set; }

        /// <summary>
        /// The minimum lifetime for subscriptions in milliseconds.
        /// </summary>
        [DataMember(Order = 11)]
        public uint MinLifetimeInterval { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the notifications are cached within the monitored items.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if monitored item cache is disabled; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// Applications must process the Session.Notication event if this is set to true.
        /// This flag improves performance by eliminating the processing involved in updating the cache.
        /// </remarks>
        [DataMember(Order = 12)]
        public bool DisableMonitoredItemCache { get; set; }

        /// <summary>
        /// Gets or sets the fast data change callback.
        /// </summary>
        /// <value>The fast data change callback.</value>
        /// <remarks>
        /// Only one callback is allowed at a time but it is more efficient to call than an event.
        /// </remarks>
        public FastDataChangeNotificationEventHandler FastDataChangeCallback { get; set; }

        /// <summary>
        /// Gets or sets the fast event callback.
        /// </summary>
        /// <value>The fast event callback.</value>
        /// <remarks>
        /// Only one callback is allowed at a time but it is more efficient to call than an event.
        /// </remarks>
        public FastEventNotificationEventHandler FastEventCallback { get; set; }

















        /// <summary>
        /// The items to monitor.
        /// </summary>
        public IEnumerable<MonitoredItem> MonitoredItems {
            get {
                lock (_cache) {
                    return new List<MonitoredItem>(_monitoredItems.Values);
                }
            }
        }

        /// <summary>
        /// Returns the number of monitored items.
        /// </summary>
        public uint MonitoredItemCount {
            get {
                lock (_cache) {
                    return (uint)_monitoredItems.Count;
                }
            }
        }

        /// <summary>
        /// A local handle assigned to the subscription
        /// </summary>
        public object Handle { get; set; }

        /// <summary>
        /// The unique identifier assigned by the server.
        /// </summary>
        public uint Id { get; private set; }

        /// <summary>
        /// Whether the subscription has been created on the server.
        /// </summary>
        public bool Created => Id != 0;

        /// <summary>
        /// The current publishing interval.
        /// </summary>
        public double CurrentPublishingInterval { get; private set; }

        /// <summary>
        /// The current keep alive count.
        /// </summary>
        public uint CurrentKeepAliveCount { get; private set; }

        /// <summary>
        /// The current lifetime count.
        /// </summary>
        public uint CurrentLifetimeCount { get; private set; }

        /// <summary>
        /// Whether publishing is currently enabled.
        /// </summary>
        public bool CurrentPublishingEnabled { get; private set; }

        /// <summary>
        /// The priority assigned to subscription when it was created.
        /// </summary>
        public byte CurrentPriority { get; private set; }

        //    /// <summary>
        //    /// The when that the last notification received was published.
        //    /// </summary>
        //    public DateTime PublishTime {
        //        get {
        //            lock (_cache) {
        //                if (_messageCache.Count > 0) {
        //                    return _messageCache.Last.Value.PublishTime;
        //                }
        //            }
        //
        //            return DateTime.MinValue;
        //        }
        //    }
        //
        //    /// <summary>
        //    /// The when that the last notification was received.
        //    /// </summary>
        //    public DateTime LastNotificationTime {
        //        get {
        //            lock (_cache) {
        //                return _lastNotificationTime;
        //            }
        //        }
        //    }
        //
        //    /// <summary>
        //    /// The sequence number assigned to the last notification message.
        //    /// </summary>
        //    public uint SequenceNumber {
        //        get {
        //            lock (_cache) {
        //                if (_messageCache.Count > 0) {
        //                    return _messageCache.Last.Value.SequenceNumber;
        //                }
        //            }
        //
        //            return 0;
        //        }
        //    }
        //
        //    /// <summary>
        //    /// The number of notifications contained in the last notification message.
        //    /// </summary>
        //    public uint NotificationCount {
        //        get {
        //            lock (_cache) {
        //                if (_messageCache.Count > 0) {
        //                    return (uint)_messageCache.Last.Value.NotificationData.Count;
        //                }
        //            }
        //
        //            return 0;
        //        }
        //    }
        //
        //    /// <summary>
        //    /// The last notification received from the server.
        //    /// </summary>
        //    public NotificationMessage LastNotification {
        //        get {
        //            lock (_cache) {
        //                if (_messageCache.Count > 0) {
        //                    return _messageCache.Last.Value;
        //                }
        //
        //                return null;
        //            }
        //        }
        //    }
        //
        //    /// <summary>
        //    /// The cached notifications.
        //    /// </summary>
        //    public IEnumerable<NotificationMessage> Notifications {
        //        get {
        //            lock (_cache) {
        //                // make a copy to ensure the state of the last cannot change during enumeration.
        //                return new List<NotificationMessage>(_messageCache);
        //            }
        //        }
        //    }
        //
        //    /// <summary>
        //    /// The sequence numbers that are available for republish requests.
        //    /// </summary>
        //    public IEnumerable<uint> AvailableSequenceNumbers {
        //        get {
        //            lock (_cache) {
        //                return _availableSequenceNumbers;
        //            }
        //        }
        //    }

        /// <summary>
        /// Sends a notification that the state of the subscription has changed.
        /// </summary>
        public void ChangesCompleted() {
            _StateChanged?.Invoke(this, new SubscriptionStateChangedEventArgs(_changeMask));

            _changeMask = SubscriptionChangeMask.None;
        }

        /// <summary>
        /// Returns true if the subscription is not receiving publishes.
        /// </summary>
        public bool PublishingStopped {
            get {
                lock (_cache) {
                    var keepAliveInterval = (int)(CurrentPublishingInterval * CurrentKeepAliveCount);

                    if (_lastNotificationTime.AddMilliseconds(keepAliveInterval + 500) < DateTime.UtcNow) {
                        return true;
                    }

                    return false;
                }
            }
        }

        /// <summary>
        /// Creates a subscription on the server.
        /// </summary>
        public async Task CreateAsync(SessionClient session) {
            VerifySubscriptionState(false);

            // create the subscription.
            var revisedKeepAliveCount = KeepAliveCount;
            var revisedLifetimeCounter = LifetimeCount;

            AdjustCounts(ref revisedKeepAliveCount, ref revisedLifetimeCounter);

            var response = await session.CreateSubscriptionAsync(null,
                PublishingInterval, revisedLifetimeCounter,
                revisedKeepAliveCount, MaxNotificationsPerPublish,
                PublishingEnabled, Priority);

            var subscriptionId = response.SubscriptionId;
            var revisedPublishingInterval = response.RevisedPublishingInterval;
            revisedKeepAliveCount = response.RevisedMaxKeepAliveCount;
            revisedLifetimeCounter = response.RevisedLifetimeCount;

            // update current state.
            Id = subscriptionId;
            CurrentPublishingInterval = revisedPublishingInterval;
            CurrentKeepAliveCount = revisedKeepAliveCount;
            CurrentLifetimeCount = revisedLifetimeCounter;
            CurrentPublishingEnabled = PublishingEnabled;
            CurrentPriority = Priority;

            StartKeepAliveTimer();

            _changeMask |= SubscriptionChangeMask.Created;

            if (KeepAliveCount != revisedKeepAliveCount) {
                Utils.Trace("For subscription {0}, Keep alive count was revised from {1} to {2}", Id, KeepAliveCount, revisedKeepAliveCount);
            }
            if (LifetimeCount != revisedLifetimeCounter) {
                Utils.Trace("For subscription {0}, Lifetime count was revised from {1} to {2}", Id, LifetimeCount, revisedLifetimeCounter);
            }
            if (PublishingInterval != revisedPublishingInterval) {
                Utils.Trace("For subscription {0}, Publishing interval was revised from {1} to {2}", Id, PublishingInterval, revisedPublishingInterval);
            }
            if (revisedLifetimeCounter < revisedKeepAliveCount * 3) {
                Utils.Trace("For subscription {0}, Revised lifetime counter (value={1}) is less than three times the keep alive count (value={2})", Id, revisedLifetimeCounter, revisedKeepAliveCount);
            }
            if (CurrentPriority == 0) {
                Utils.Trace("For subscription {0}, the priority was set to 0.", Id);
            }

            await CreateItemsAsync(session);

            ChangesCompleted();
        }

        /// <summary>
        /// Deletes a subscription on the server.
        /// </summary>
        public async Task DeleteAsync(SessionClient session, bool silent = false) {
            if (!silent) {
                VerifySubscriptionState(true);
            }

            // nothing to do if not created.
            if (!Created) {
                return;
            }

            try {
                // stop the publish timer.
                if (_publishTimer != null) {
                    _publishTimer.Dispose();
                    _publishTimer = null;
                }

                // delete the subscription.
                UInt32Collection subscriptionIds = new uint[] { Id };
                var response = await session.DeleteSubscriptionsAsync(null, subscriptionIds);
                var results = response.Results;
                var diagnosticInfos = response.DiagnosticInfos;
                var responseHeader = response.ResponseHeader;

                // validate response.
                ClientBase.ValidateResponse(results, subscriptionIds);
                ClientBase.ValidateDiagnosticInfos(diagnosticInfos, subscriptionIds);

                if (StatusCode.IsBad(results[0])) {
                    throw new ServiceResultException(
                        ClientBase.GetResult(results[0], 0, diagnosticInfos, responseHeader));
                }
            }
            // supress exception if silent flag is set.
            catch (Exception e) {
                if (!silent) {
                    throw new ServiceResultException(e, StatusCodes.BadUnexpectedError);
                }
            }
            // always put object in disconnected state even if an error occurs.
            finally {
                Id = 0;
                CurrentPublishingInterval = 0;
                CurrentKeepAliveCount = 0;
                CurrentPublishingEnabled = false;
                CurrentPriority = 0;
                // update items.
                lock (_cache) {
                    foreach (var monitoredItem in _monitoredItems.Values) {
                        monitoredItem.SetDeleteResult(StatusCodes.Good, -1, null, null);
                    }
                }
                _deletedItems.Clear();
                _changeMask |= SubscriptionChangeMask.Deleted;
            }
            ChangesCompleted();
        }

        /// <summary>
        /// Modifies a subscription on the server.
        /// </summary>
        public async Task ModifyAsync(SessionClient session) {
            VerifySubscriptionState(true);

            // modify the subscription.
            var revisedKeepAliveCount = KeepAliveCount;
            var revisedLifetimeCounter = LifetimeCount;

            AdjustCounts(ref revisedKeepAliveCount, ref revisedLifetimeCounter);

            var response = await session.ModifySubscriptionAsync(
                null, Id, PublishingInterval, revisedLifetimeCounter,
                revisedKeepAliveCount, MaxNotificationsPerPublish, Priority);

            var revisedPublishingInterval = response.RevisedPublishingInterval;
            revisedLifetimeCounter = response.RevisedLifetimeCount;
            revisedKeepAliveCount = response.RevisedMaxKeepAliveCount;

            // update current state.
            CurrentPublishingInterval = revisedPublishingInterval;
            CurrentKeepAliveCount = revisedKeepAliveCount;
            CurrentLifetimeCount = revisedLifetimeCounter;
            CurrentPriority = Priority;

            _changeMask |= SubscriptionChangeMask.Modified;
            ChangesCompleted();
        }

        /// <summary>
        /// Changes the publishing enabled state for the subscription.
        /// </summary>
        public async Task SetPublishingModeAsync(SessionClient session, bool enabled) {
            VerifySubscriptionState(true);

            // modify the subscription.
            UInt32Collection subscriptionIds = new uint[] { Id };
            var response = await session.SetPublishingModeAsync(
                null, enabled, subscriptionIds);

            var responseHeader = response.ResponseHeader;
            var results = response.Results;
            var diagnosticInfos = response.DiagnosticInfos;

            // validate response.
            ClientBase.ValidateResponse(results, subscriptionIds);
            ClientBase.ValidateDiagnosticInfos(diagnosticInfos, subscriptionIds);

            if (StatusCode.IsBad(results[0])) {
                throw new ServiceResultException(
                    ClientBase.GetResult(results[0], 0, diagnosticInfos, responseHeader));
            }

            // update current state.
            CurrentPublishingEnabled = PublishingEnabled = enabled;
            _changeMask |= SubscriptionChangeMask.Modified;
            ChangesCompleted();
        }


        /// <summary>
        /// Applies any changes to the subscription items.
        /// </summary>
        public async Task ApplyChangesAsync(SessionClient session) {
            await DeleteItemsAsync(session);
            await ModifyItemsAsync(session);
            await CreateItemsAsync(session);
        }

        /// <summary>
        /// Creates all items that have not already been created.
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        public async Task<IEnumerable<MonitoredItem>> CreateItemsAsync(SessionClient session) {
            VerifySubscriptionState(true);

            await ResolveItemNodeIdsAsync(session);
            var requestItems = new MonitoredItemCreateRequestCollection();
            var itemsToCreate = new List<MonitoredItem>();
            lock (_cache) {
                foreach (var monitoredItem in _monitoredItems.Values) {
                    // ignore items that have been created.
                    if (monitoredItem.Status.Created) {
                        continue;
                    }

                    // build item request.
                    var request = new MonitoredItemCreateRequest();

                    request.ItemToMonitor.NodeId = monitoredItem.ResolvedNodeId;
                    request.ItemToMonitor.AttributeId = monitoredItem.AttributeId;
                    request.ItemToMonitor.IndexRange = monitoredItem.IndexRange;
                    request.ItemToMonitor.DataEncoding = monitoredItem.Encoding;

                    request.MonitoringMode = monitoredItem.MonitoringMode;

                    request.RequestedParameters.ClientHandle = monitoredItem.ClientHandle;
                    request.RequestedParameters.SamplingInterval = monitoredItem.SamplingInterval;
                    request.RequestedParameters.QueueSize = monitoredItem.QueueSize;
                    request.RequestedParameters.DiscardOldest = monitoredItem.DiscardOldest;

                    if (monitoredItem.Filter != null) {
                        request.RequestedParameters.Filter = new ExtensionObject(monitoredItem.Filter);
                    }

                    requestItems.Add(request);
                    itemsToCreate.Add(monitoredItem);
                }
            }
            if (requestItems.Count == 0) {
                return itemsToCreate;
            }
            // create the monitored items.
            var response = await session.CreateMonitoredItemsAsync(null, Id, TimestampsToReturn,
                requestItems);
            var results = response.Results;
            var diagnosticInfos = response.DiagnosticInfos;
            var responseHeader = response.ResponseHeader;

            ClientBase.ValidateResponse(results, itemsToCreate);
            ClientBase.ValidateDiagnosticInfos(diagnosticInfos, itemsToCreate);

            // update results.
            for (var ii = 0; ii < results.Count; ii++) {
                itemsToCreate[ii].SetCreateResult(requestItems[ii], results[ii], ii, diagnosticInfos, responseHeader);
            }

            _changeMask |= SubscriptionChangeMask.ItemsCreated;
            ChangesCompleted();

            // return the list of items affected by the change.
            return itemsToCreate;
        }

        /// <summary>
        /// Modifies all monitored items that changed
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        public async Task<IEnumerable<MonitoredItem>> ModifyItemsAsync(SessionClient session) {
            VerifySubscriptionState(true);

            var requestItems = new MonitoredItemModifyRequestCollection();
            var itemsToModify = new List<MonitoredItem>();

            lock (_cache) {
                foreach (var monitoredItem in _monitoredItems.Values) {
                    // ignore items that have been created or modified.
                    if (!monitoredItem.Status.Created || !monitoredItem.AttributesModified) {
                        continue;
                    }

                    // build item request.
                    var request = new MonitoredItemModifyRequest {
                        MonitoredItemId = monitoredItem.Status.Id
                    };
                    request.RequestedParameters.ClientHandle = monitoredItem.ClientHandle;
                    request.RequestedParameters.SamplingInterval = monitoredItem.SamplingInterval;
                    request.RequestedParameters.QueueSize = monitoredItem.QueueSize;
                    request.RequestedParameters.DiscardOldest = monitoredItem.DiscardOldest;

                    if (monitoredItem.Filter != null) {
                        request.RequestedParameters.Filter = new ExtensionObject(monitoredItem.Filter);
                    }

                    requestItems.Add(request);
                    itemsToModify.Add(monitoredItem);
                }
            }

            if (requestItems.Count == 0) {
                return itemsToModify;
            }

            var response = await session.ModifyMonitoredItemsAsync(
                null, Id, TimestampsToReturn, requestItems);

            // modify the subscription.
            var results = response.Results;
            var diagnosticInfos = response.DiagnosticInfos;
            var responseHeader = response.ResponseHeader;

            ClientBase.ValidateResponse(results, itemsToModify);
            ClientBase.ValidateDiagnosticInfos(diagnosticInfos, itemsToModify);

            // update results.
            for (var ii = 0; ii < results.Count; ii++) {
                itemsToModify[ii].SetModifyResult(requestItems[ii], results[ii], ii,
                    diagnosticInfos, responseHeader);
            }

            _changeMask |= SubscriptionChangeMask.ItemsCreated;
            ChangesCompleted();

            // return the list of items affected by the change.
            return itemsToModify;
        }

        /// <summary>
        /// Deletes all items that are removed
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        public async Task<IEnumerable<MonitoredItem>> DeleteItemsAsync(SessionClient session) {
            VerifySubscriptionState(true);

            if (_deletedItems.Count == 0) {
                return new List<MonitoredItem>();
            }

            var itemsToDelete = _deletedItems;
            _deletedItems = new List<MonitoredItem>();
            var monitoredItemIds = new UInt32Collection();
            foreach (var monitoredItem in itemsToDelete) {
                monitoredItemIds.Add(monitoredItem.Status.Id);
            }
            var response = await session.DeleteMonitoredItemsAsync(null, Id,
                monitoredItemIds);

            var results = response.Results;
            var diagnosticInfos = response.DiagnosticInfos;
            var responseHeader = response.ResponseHeader;

            ClientBase.ValidateResponse(results, monitoredItemIds);
            ClientBase.ValidateDiagnosticInfos(diagnosticInfos, monitoredItemIds);

            // update results.
            for (var ii = 0; ii < results.Count; ii++) {
                itemsToDelete[ii].SetDeleteResult(results[ii], ii, diagnosticInfos, responseHeader);
            }

            _changeMask |= SubscriptionChangeMask.ItemsDeleted;
            ChangesCompleted();

            // return the list of items affected by the change.
            return itemsToDelete;
        }

        /// <summary>
        /// Resolves all relative paths to nodes on the server.
        /// </summary>
        public async Task ResolveItemNodeIdsAsync(SessionClient session) {
            VerifySubscriptionState(true);

            // collect list of browse paths.
            var browsePaths = new BrowsePathCollection();
            var itemsToBrowse = new List<MonitoredItem>();

            lock (_cache) {
                foreach (var monitoredItem in _monitoredItems.Values) {
                    if (!string.IsNullOrEmpty(monitoredItem.RelativePath) && NodeId.IsNull(monitoredItem.ResolvedNodeId)) {
                        // cannot change the relative path after an item is created.
                        if (monitoredItem.Created) {
                            throw new ServiceResultException(StatusCodes.BadInvalidState, "Cannot modify item path after it is created.");
                        }

                        var browsePath = new BrowsePath {
                            StartingNode = monitoredItem.StartNodeId
                        };

                        // parse the relative path.
                        try {
                            browsePath.RelativePath = RelativePath.Parse(monitoredItem.RelativePath, Session.TypeTree);
                        }
                        catch (Exception e) {
                            monitoredItem.SetError(new ServiceResult(e));
                            continue;
                        }

                        browsePaths.Add(browsePath);
                        itemsToBrowse.Add(monitoredItem);
                    }
                }
            }

            // nothing to do.
            if (browsePaths.Count == 0) {
                return;
            }

            // translate browse paths.
            var response = await session.TranslateBrowsePathsToNodeIdsAsync(null, browsePaths);

            var results = response.Results;
            var diagnosticInfos = response.DiagnosticInfos;
            var responseHeader = response.ResponseHeader;
            ClientBase.ValidateResponse(results, browsePaths);
            ClientBase.ValidateDiagnosticInfos(diagnosticInfos, browsePaths);

            // update results.
            for (var ii = 0; ii < results.Count; ii++) {
                itemsToBrowse[ii].SetResolvePathResult(results[ii], ii, diagnosticInfos, responseHeader);
            }

            _changeMask |= SubscriptionChangeMask.ItemsModified;
        }

        /// <summary>
        /// Deletes all items that have been marked for deletion.
        /// </summary>
        public async Task<List<ServiceResult>> SetMonitoringModeAsync(
            SessionClient session, MonitoringMode monitoringMode,
            IList<MonitoredItem> monitoredItems) {
            if (monitoredItems == null) {
                throw new ArgumentNullException("monitoredItems");
            }

            VerifySubscriptionState(true);

            if (monitoredItems.Count == 0) {
                return null;
            }

            // get list of items to update.
            var monitoredItemIds = new UInt32Collection();

            foreach (var monitoredItem in monitoredItems) {
                monitoredItemIds.Add(monitoredItem.Status.Id);
            }

            var response = await session.SetMonitoringModeAsync(null, Id, monitoringMode,
                monitoredItemIds);
            var results = response.Results;
            var diagnosticInfos = response.DiagnosticInfos;
            var responseHeader = response.ResponseHeader;

            ClientBase.ValidateResponse(results, monitoredItemIds);
            ClientBase.ValidateDiagnosticInfos(diagnosticInfos, monitoredItemIds);

            // update results.
            var noErrors = true;
            var errors = new List<ServiceResult>();

            for (var ii = 0; ii < results.Count; ii++) {
                ServiceResult error = null;
                if (StatusCode.IsBad(results[ii])) {
                    error = ClientBase.GetResult(results[ii], ii, diagnosticInfos, responseHeader);
                    noErrors = false;
                }
                else {
                    monitoredItems[ii].MonitoringMode = monitoringMode;
                    monitoredItems[ii].Status.SetMonitoringMode(monitoringMode);
                }
                errors.Add(error);
            }

            // raise state changed event.
            _changeMask |= SubscriptionChangeMask.ItemsModified;
            ChangesCompleted();

            // return null list if no errors occurred.
            if (noErrors) {
                return null;
            }

            return errors;
        }

        /// <summary>
        /// Adds the notification message to internal cache.
        /// </summary>
        public void SaveMessageInCache(
            IList<uint> availableSequenceNumbers,
            NotificationMessage message,
            IList<string> stringTable) {
            EventHandler callback = null;

            lock (_cache) {
                if (availableSequenceNumbers != null) {
                    _availableSequenceNumbers = availableSequenceNumbers;
                }

                if (message == null) {
                    return;
                }

                // check if a publish error was previously reported.
                if (PublishingStopped) {
                    callback = _PublishStatusChanged;
                    TraceState("PUBLISHING RECOVERED");
                }

                _lastNotificationTime = DateTime.UtcNow;

                // save the string table that came with notification.
                message.StringTable = new List<string>(stringTable);

                // create queue for the first time.
                if (_incomingMessages == null) {
                    _incomingMessages = new LinkedList<IncomingMessage>();
                }

                // find or create an entry for the incoming sequence number.
                IncomingMessage entry = null;
                var node = _incomingMessages.Last;

                while (node != null) {
                    entry = node.Value;
                    var previous = node.Previous;

                    if (entry.SequenceNumber == message.SequenceNumber) {
                        entry.Timestamp = DateTime.UtcNow;
                        break;
                    }

                    if (entry.SequenceNumber < message.SequenceNumber) {
                        entry = new IncomingMessage {
                            SequenceNumber = message.SequenceNumber,
                            Timestamp = DateTime.UtcNow
                        };
                        _incomingMessages.AddAfter(node, entry);
                        break;
                    }

                    node = previous;
                    entry = null;
                }

                if (entry == null) {
                    entry = new IncomingMessage {
                        SequenceNumber = message.SequenceNumber,
                        Timestamp = DateTime.UtcNow
                    };
                    _incomingMessages.AddLast(entry);
                }

                // check for keep alive.
                if (message.NotificationData.Count > 0) {
                    entry.Message = message;
                    entry.Processed = false;
                }

                // fill in any gaps in the queue
                node = _incomingMessages.First;

                while (node != null) {
                    entry = node.Value;
                    var next = node.Next;

                    if (next != null && next.Value.SequenceNumber > entry.SequenceNumber + 1) {
                        var placeholder = new IncomingMessage {
                            SequenceNumber = entry.SequenceNumber + 1,
                            Timestamp = DateTime.UtcNow
                        };
                        node = _incomingMessages.AddAfter(node, placeholder);
                        continue;
                    }

                    node = next;
                }

                // clean out processed values.
                node = _incomingMessages.First;

                while (node != null) {
                    entry = node.Value;
                    var next = node.Next;

                    // can only pull off processed or expired messages.
                    if (!entry.Processed && !(entry.Republished && entry.Timestamp.AddSeconds(10) < DateTime.UtcNow)) {
                        break;
                    }

                    if (next != null) {
                        _incomingMessages.Remove(node);
                    }

                    node = next;
                }

                // process messages.
                Task.Run(() => {
                    Interlocked.Increment(ref _outstandingMessageWorkers);
                    OnMessageReceived(null);
                });
            }

            // send notification that publishing has recovered.
            if (callback != null) {
                try {
                    callback(this, null);
                }
                catch (Exception e) {
                    Utils.Trace(e, "Error while raising PublishStateChanged event.");
                }
            }
        }












        /// <summary>
        /// Dumps the current state of the session queue.
        /// </summary>
        internal void TraceState(string context) {
            if ((Utils.TraceMask & Utils.TraceMasks.Information) == 0) {
                return;
            }

            var buffer = new StringBuilder();

            buffer.AppendFormat("Subscription {0}", context);
            buffer.AppendFormat(", Id={0}", Id);
            buffer.AppendFormat(", LastNotificationTime={0:HH:mm:ss}", _lastNotificationTime);

            buffer.AppendFormat(", PublishingInterval={0}", CurrentPublishingInterval);
            buffer.AppendFormat(", KeepAliveCount={0}", CurrentKeepAliveCount);
            buffer.AppendFormat(", PublishingEnabled={0}", CurrentPublishingEnabled);
            buffer.AppendFormat(", MonitoredItemCount={0}", MonitoredItemCount);

            Utils.Trace("{0}", buffer.ToString());
        }



        /// <summary>
        /// Processes the incoming messages.
        /// </summary>
        private void OnMessageReceived(SessionClient session, object state) {
            try {
                uint subscriptionId = 0;
                EventHandler callback = null;

                // get list of new messages to process.
                List<NotificationMessage> messagesToProcess = null;

                // get list of new messages to republish.
                List<IncomingMessage> messagesToRepublish = null;

                lock (_cache) {
                    for (var ii = _incomingMessages.First; ii != null; ii = ii.Next) {
                        // update monitored items with unprocessed messages.
                        if (ii.Value.Message != null && !ii.Value.Processed) {
                            if (messagesToProcess == null) {
                                messagesToProcess = new List<NotificationMessage>();
                            }

                            messagesToProcess.Add(ii.Value.Message);

                            // remove the oldest items.
                            while (_messageCache.Count > _maxMessageCount) {
                                _messageCache.RemoveFirst();
                            }

                            _messageCache.AddLast(ii.Value.Message);
                            ii.Value.Processed = true;
                        }

                        // check for missing messages.
                        if (ii.Next != null && ii.Value.Message == null && !ii.Value.Processed && !ii.Value.Republished) {
                            if (ii.Value.Timestamp.AddSeconds(2) < DateTime.UtcNow) {
                                if (messagesToRepublish == null) {
                                    messagesToRepublish = new List<IncomingMessage>();
                                }

                                messagesToRepublish.Add(ii.Value);
                                ii.Value.Republished = true;
                            }
                        }
                    }

                    subscriptionId = Id;
                    callback = _PublishStatusChanged;
                }

                if (callback != null) {
                    try {
                        callback(this, null);
                    }
                    catch (Exception e) {
                        Utils.Trace(e, "Error while raising PublishStateChanged event.");
                    }
                }

                // process new messages.
                if (messagesToProcess != null) {
                    var datachangeCallback = FastDataChangeCallback;
                    var eventCallback = FastEventCallback;
                    var noNotificationsReceived = 0;

                    for (var ii = 0; ii < messagesToProcess.Count; ii++) {
                        var message = messagesToProcess[ii];
                        noNotificationsReceived = 0;
                        try {
                            for (var jj = 0; jj < message.NotificationData.Count; jj++) {
                                if (message.NotificationData[jj].Body is DataChangeNotification datachange) {
                                    noNotificationsReceived += datachange.MonitoredItems.Count;

                                    if (!DisableMonitoredItemCache) {
                                        SaveDataChange(message, datachange, message.StringTable);
                                    }

                                    datachangeCallback?.Invoke(this, datachange, message.StringTable);
                                }


                                if (message.NotificationData[jj].Body is EventNotificationList events) {
                                    noNotificationsReceived += events.Events.Count;

                                    if (!DisableMonitoredItemCache) {
                                        SaveEvents(message, events, message.StringTable);
                                    }

                                    eventCallback?.Invoke(this, events, message.StringTable);
                                }


                                if (message.NotificationData[jj].Body is StatusChangeNotification statusChanged) {
                                    Utils.Trace("StatusChangeNotification received with Status = {0} for SubscriptionId={1}.", statusChanged.Status.ToString(), Id);
                                }
                            }
                        }
                        catch (Exception e) {
                            Utils.Trace(e, "Error while processing incoming message #{0}.", message.SequenceNumber);
                        }

                        if (MaxNotificationsPerPublish != 0 && noNotificationsReceived > MaxNotificationsPerPublish) {
                            Utils.Trace("For subscription {0}, more notifications were received={1} than the max notifications per publish value={2}", Id, noNotificationsReceived, MaxNotificationsPerPublish);
                        }
                    }
                }

                // do any re-publishes.
                if (messagesToRepublish != null && session != null && subscriptionId != 0) {
                    for (var ii = 0; ii < messagesToRepublish.Count; ii++) {
                        if (!session.Republish(subscriptionId, messagesToRepublish[ii].SequenceNumber)) {
                            messagesToRepublish[ii].Republished = false;
                        }
                    }
                }
            }
            catch (Exception e) {
                Utils.Trace(e, "Error while processing incoming messages.");
            }
            Interlocked.Decrement(ref _outstandingMessageWorkers);
        }

        /// <summary>
        /// Get the number of outstanding message workers
        /// </summary>
        public int OutstandingMessageWorkers => _outstandingMessageWorkers;

        /// <summary>
        /// Adds an item to the subscription.
        /// </summary>
        public void AddItem(MonitoredItem monitoredItem) {
            if (monitoredItem == null) {
                throw new ArgumentNullException("monitoredItem");
            }

            lock (_cache) {
                if (_monitoredItems.ContainsKey(monitoredItem.ClientHandle)) {
                    return;
                }
                _monitoredItems.Add(monitoredItem.ClientHandle, monitoredItem);
                monitoredItem.Subscription = this;
            }

            _changeMask |= SubscriptionChangeMask.ItemsAdded;
            ChangesCompleted();
        }

        /// <summary>
        /// Adds an item to the subscription.
        /// </summary>
        public void AddItems(IEnumerable<MonitoredItem> monitoredItems) {
            if (monitoredItems == null) {
                throw new ArgumentNullException("monitoredItems");
            }
            var added = false;
            lock (_cache) {
                foreach (var monitoredItem in monitoredItems) {
                    if (!_monitoredItems.ContainsKey(monitoredItem.ClientHandle)) {
                        _monitoredItems.Add(monitoredItem.ClientHandle, monitoredItem);
                        monitoredItem.Subscription = this;
                        added = true;
                    }
                }
            }
            if (added) {
                _changeMask |= SubscriptionChangeMask.ItemsAdded;
                ChangesCompleted();
            }
        }

        /// <summary>
        /// Removes an item from the subscription.
        /// </summary>
        public void RemoveItem(MonitoredItem monitoredItem) {
            if (monitoredItem == null) {
                throw new ArgumentNullException("monitoredItem");
            }
            lock (_cache) {
                if (!_monitoredItems.Remove(monitoredItem.ClientHandle)) {
                    return;
                }

                monitoredItem.Subscription = null;
            }

            if (monitoredItem.Status.Created) {
                _deletedItems.Add(monitoredItem);
            }

            _changeMask |= SubscriptionChangeMask.ItemsRemoved;
            ChangesCompleted();
        }

        /// <summary>
        /// Removes an item from the subscription.
        /// </summary>
        public void RemoveItems(IEnumerable<MonitoredItem> monitoredItems) {
            if (monitoredItems == null) {
                throw new ArgumentNullException("monitoredItems");
            }

            var changed = false;

            lock (_cache) {
                foreach (var monitoredItem in monitoredItems) {
                    if (_monitoredItems.Remove(monitoredItem.ClientHandle)) {
                        monitoredItem.Subscription = null;

                        if (monitoredItem.Status.Created) {
                            _deletedItems.Add(monitoredItem);
                        }

                        changed = true;
                    }
                }
            }

            if (changed) {
                _changeMask |= SubscriptionChangeMask.ItemsRemoved;
                ChangesCompleted();
            }
        }

        // /// <summary>
        // /// Returns the monitored item identified by the client handle.
        // /// </summary>
        // public MonitoredItem FindItemByClientHandle(uint clientHandle) {
        //     lock (_cache) {
        //         MonitoredItem monitoredItem = null;
        //
        //         if (_monitoredItems.TryGetValue(clientHandle, out monitoredItem)) {
        //             return monitoredItem;
        //         }
        //
        //         return null;
        //     }
        // }

        /// <summary>
        /// Tells the server to refresh all conditions being monitored by
        /// the subscription.
        /// </summary>
        public async Task ConditionRefreshAsync() {
            VerifySubscriptionState(true);
            await Session.CallAsync(null, new CallMethodRequestCollection {
                new CallMethodRequest {
                    ObjectId = ObjectTypeIds.ConditionType,
                    MethodId = MethodIds.ConditionType_ConditionRefresh,
                    InputArguments = new VariantCollection {
                        new Variant(Id)
                    }
                }
            });
        }

        /// <summary>
        /// Throws an exception if the subscription is not in the correct state.
        /// </summary>
        private void VerifySubscriptionState(bool created) {
            if (created && Id == 0) {
                throw new ServiceResultException(StatusCodes.BadInvalidState,
                    "Subscription has not been created.");
            }
            if (!created && Id != 0) {
                throw new ServiceResultException(StatusCodes.BadInvalidState,
                    "Subscription has alredy been created.");
            }
        }

        /// <summary>
        /// Saves a data change in the monitored item cache.
        /// </summary>
        private void SaveDataChange(NotificationMessage message, DataChangeNotification notifications, IList<string> stringTable) {
            // check for empty monitored items list.
            if (notifications.MonitoredItems == null || notifications.MonitoredItems.Count == 0) {
                Utils.Trace("Publish response contains empty MonitoredItems list for SubscritpionId = {0}.", Id);
            }

            for (var ii = 0; ii < notifications.MonitoredItems.Count; ii++) {
                var notification = notifications.MonitoredItems[ii];

                // lookup monitored item,
                MonitoredItem monitoredItem = null;

                lock (_cache) {
                    if (!_monitoredItems.TryGetValue(notification.ClientHandle, out monitoredItem)) {
                        Utils.Trace("Publish response contains invalid MonitoredItem.SubscritpionId = {0}, ClientHandle = {1}", Id, notification.ClientHandle);
                        continue;
                    }
                }

                // save the message.
                notification.Message = message;

                // get diagnostic info.
                if (notifications.DiagnosticInfos.Count > ii) {
                    notification.DiagnosticInfo = notifications.DiagnosticInfos[ii];
                }

                // save in cache.
                monitoredItem.SaveValueInCache(notification);
            }
        }

        /// <summary>
        /// Saves events in the monitored item cache.
        /// </summary>
        private void SaveEvents(NotificationMessage message, EventNotificationList notifications, IList<string> stringTable) {
            for (var ii = 0; ii < notifications.Events.Count; ii++) {
                var eventFields = notifications.Events[ii];

                MonitoredItem monitoredItem = null;

                lock (_cache) {
                    if (!_monitoredItems.TryGetValue(eventFields.ClientHandle, out monitoredItem)) {
                        Utils.Trace("Publish response contains invalid MonitoredItem.SubscritpionId = {0}, ClientHandle = {1}", Id, eventFields.ClientHandle);
                        continue;
                    }
                }

                // save the message.
                eventFields.Message = message;

                // save in cache.
                monitoredItem.SaveValueInCache(eventFields);
            }
        }



        /// <summary>
        /// Ensures sensible values for the counts.
        /// </summary>
        private void AdjustCounts(ref uint keepAliveCount, ref uint lifetimeCount) {
            // keep alive count must be at least 1.
            if (keepAliveCount == 0) {
                keepAliveCount = 1;
            }

            // ensure the lifetime is sensible given the sampling interval.
            if (PublishingInterval > 0) {
                var minLifetimeCount = (uint)(MinLifetimeInterval / PublishingInterval);

                if (lifetimeCount < minLifetimeCount) {
                    lifetimeCount = minLifetimeCount;

                    if (MinLifetimeInterval % PublishingInterval != 0) {
                        lifetimeCount++;
                    }

                    Utils.Trace("Adjusted LifetimeCount to value={0}, for subscription {1}. ", lifetimeCount, Id);
                }
            }

            // don't know what the sampling interval will be - use something large enough
            // to ensure the user does not experience unexpected drop outs.
            else {
                Utils.Trace("Adjusted LifetimeCount from value={0}, to value={1}, for subscription {2}. ", lifetimeCount, 1000, Id);
                lifetimeCount = 1000;
            }

            // lifetime must be greater than the keep alive count.
            if (lifetimeCount < keepAliveCount) {
                Utils.Trace("Adjusted LifetimeCount from value={0}, to value={1}, for subscription {2}. ", lifetimeCount, keepAliveCount, Id);
                lifetimeCount = keepAliveCount;
            }
        }

        /// <summary>
        /// Starts a timer to ensure publish requests are sent frequently enough to detect network interruptions.
        /// </summary>
        private void StartKeepAliveTimer() {
            // stop the publish timer.
            if (_publishTimer != null) {
                _publishTimer.Dispose();
                _publishTimer = null;
            }

            lock (_cache) {
                _lastNotificationTime = DateTime.MinValue;
            }

            var keepAliveInterval = (int)(CurrentPublishingInterval * CurrentKeepAliveCount);

            _lastNotificationTime = DateTime.UtcNow;
            _publishTimer = new Timer(OnKeepAlive, keepAliveInterval, keepAliveInterval, keepAliveInterval);

            // send initial publish.
            Session.BeginPublish(keepAliveInterval * 3);
        }

        /// <summary>
        /// Checks if a notification has arrived. Sends a publish if it has not.
        /// </summary>
        private void OnKeepAlive(object state) {
            // check if a publish has arrived.
            EventHandler callback = null;

            lock (_cache) {
                if (!PublishingStopped) {
                    return;
                }

                callback = _PublishStatusChanged;
                _publishLateCount++;
            }

            TraceState("PUBLISHING STOPPED");

            if (callback != null) {
                try {
                    callback(this, null);
                }
                catch (Exception e) {
                    Utils.Trace(e, "Error while raising PublishStateChanged event.");
                }
            }
        }

        private List<MonitoredItem> _deletedItems;
        private event SubscriptionStateChangedEventHandler _StateChanged;

        private SubscriptionChangeMask _changeMask;
        private Timer _publishTimer;
        private DateTime _lastNotificationTime;
        private int _publishLateCount;
        private event EventHandler _PublishStatusChanged;

        private readonly object _cache = new object();
        private LinkedList<NotificationMessage> _messageCache;
        private IList<uint> _availableSequenceNumbers;
        private int _maxMessageCount;
        private SortedDictionary<uint, MonitoredItem> _monitoredItems;
        private int _outstandingMessageWorkers;

        /// <summary>
        /// A message received from the server cached until is processed or discarded.
        /// </summary>
        private class IncomingMessage {
            public uint SequenceNumber;
            public DateTime Timestamp;
            public NotificationMessage Message;
            public bool Processed;
            public bool Republished;
        }

        private LinkedList<IncomingMessage> _incomingMessages;

        private static long s_globalSubscriptionCounter;
    }

    /// <summary>
    /// The delegate used to receive data change notifications via a direct function call instead of a .NET Event.
    /// </summary>
    public delegate void FastDataChangeNotificationEventHandler(Subscription subscription, DataChangeNotification notification, IList<string> stringTable);

    /// <summary>
    /// The delegate used to receive event notifications via a direct function call instead of a .NET Event.
    /// </summary>
    public delegate void FastEventNotificationEventHandler(Subscription subscription, EventNotificationList notification, IList<string> stringTable);

    #region SubscriptionStateChangedEventArgs Class
    /// <summary>
    /// The event arguments provided when the state of a subscription changes.
    /// </summary>
    public class SubscriptionStateChangedEventArgs : EventArgs {
        #region Constructors
        /// <summary>
        /// Creates a new instance.
        /// </summary>
        internal SubscriptionStateChangedEventArgs(SubscriptionChangeMask changeMask) {
            Status = changeMask;
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// The changes that have affected the subscription.
        /// </summary>
        public SubscriptionChangeMask Status { get; }
        #endregion

        #region Private Fields
        #endregion
    }

    /// <summary>
    /// The delegate used to receive subscription state change notifications.
    /// </summary>
    public delegate void SubscriptionStateChangedEventHandler(Subscription subscription, SubscriptionStateChangedEventArgs e);
    #endregion

    /// <summary>
    /// A collection of subscriptions.
    /// </summary>
    [CollectionDataContract(Name = "ListOfSubscription", Namespace = Namespaces.OpcUaXsd, ItemName = "Subscription")]
    public partial class SubscriptionCollection : List<Subscription> {
        #region Constructors
        /// <summary>
        /// Initializes an empty collection.
        /// </summary>
        public SubscriptionCollection() { }

        /// <summary>
        /// Initializes the collection from another collection.
        /// </summary>
        /// <param name="collection">The existing collection to use as the basis of creating this collection</param>
        public SubscriptionCollection(IEnumerable<Subscription> collection) : base(collection) { }

        /// <summary>
        /// Initializes the collection with the specified capacity.
        /// </summary>
        /// <param name="capacity">The max. capacity of the collection</param>
        public SubscriptionCollection(int capacity) : base(capacity) { }
        #endregion
    }
}
