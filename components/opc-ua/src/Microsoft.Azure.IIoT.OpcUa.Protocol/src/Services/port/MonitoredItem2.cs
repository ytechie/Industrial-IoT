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

using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.Azure.IIoT.OpcUa.Protocol {
    /// <summary>
    /// A monitored item.
    /// </summary>
    [DataContract(Namespace = Namespaces.OpcUaXsd)]
    [KnownType(typeof(DataChangeFilter))]
    [KnownType(typeof(EventFilter))]
    [KnownType(typeof(AggregateFilter))]
    public class MonitoredItem {
        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="MonitoredItem"/> class.
        /// </summary>
        public MonitoredItem() {
            Initialize();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MonitoredItem"/> class.
        /// </summary>
        /// <param name="clientHandle">The client handle. The caller must ensure it uniquely identifies the monitored item.</param>
        public MonitoredItem(uint clientHandle) {
            Initialize();
            ClientHandle = clientHandle;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MonitoredItem"/> class.
        /// </summary>
        /// <param name="template">The template used to specify the monitoring parameters.</param>
        public MonitoredItem(MonitoredItem template) : this(template, false) {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MonitoredItem"/> class.
        /// </summary>
        /// <param name="template">The template used to specify the monitoring parameters.</param>
        /// <param name="copyEventHandlers">if set to <c>true</c> the event handlers are copied.</param>
        public MonitoredItem(MonitoredItem template, bool copyEventHandlers) {
            Initialize();

            if (template != null) {
                var displayName = template.DisplayName;

                if (displayName != null) {
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
                }

                Handle = template.Handle;
                DisplayName = Opc.Ua.Utils.Format("{0} {1}", displayName, ClientHandle);
                StartNodeId = template.StartNodeId;
                _relativePath = template._relativePath;
                AttributeId = template.AttributeId;
                IndexRange = template.IndexRange;
                Encoding = template.Encoding;
                MonitoringMode = template.MonitoringMode;
                _samplingInterval = template._samplingInterval;
                _filter = (MonitoringFilter)Opc.Ua.Utils.Clone(template._filter);
                _queueSize = template._queueSize;
                _discardOldest = template._discardOldest;
                AttributesModified = true;

                if (copyEventHandlers) {
                    _Notification = template._Notification;
                }

                // this ensures the state is consistent with the node class.
                NodeClass = template._nodeClass;
            }
        }

        /// <summary>
        /// Called by the .NET framework during deserialization.
        /// </summary>
        [OnDeserializing]
        private void Initialize(StreamingContext context) {
            // object initializers are not called during deserialization.
            _cache = new object();

            Initialize();
        }

        /// <summary>
        /// Sets the private members to default values.
        /// </summary>
        private void Initialize() {
            StartNodeId = null;
            _relativePath = null;
            ClientHandle = 0;
            AttributeId = Attributes.Value;
            IndexRange = null;
            Encoding = null;
            MonitoringMode = MonitoringMode.Reporting;
            _samplingInterval = -1;
            _filter = null;
            _queueSize = 0;
            _discardOldest = true;
            AttributesModified = true;
            Status = new MonitoredItemStatus2();

            // this ensures the state is consistent with the node class.
            NodeClass = NodeClass.Variable;

            // assign a unique handle.
            ClientHandle = Opc.Ua.Utils.IncrementIdentifier(ref s_GlobalClientHandle);
        }
        #endregion

        #region Persistent Properties
        /// <summary>
        /// A display name for the monitored item.
        /// </summary>
        [DataMember(Order = 1)]
        public string DisplayName { get; set; }


        /// <summary>
        /// The start node for the browse path that identifies the node to monitor.
        /// </summary>
        [DataMember(Order = 2)]
        public NodeId StartNodeId { get; set; }

        /// <summary>
        /// The relative path from the browse path to the node to monitor.
        /// </summary>
        /// <remarks>
        /// A null or empty string specifies that the start node id should be monitored.
        /// </remarks>
        [DataMember(Order = 3)]
        public string RelativePath {
            get => _relativePath;

            set {
                // clear resolved path if relative path has changed.
                if (_relativePath != value) {
                    _resolvedNodeId = null;
                }

                _relativePath = value;
            }
        }

        /// <summary>
        /// The node class of the node being monitored (affects the type of filter available).
        /// </summary>
        [DataMember(Order = 4)]
        public NodeClass NodeClass {
            get => _nodeClass;

            set {
                if (_nodeClass != value) {
                    if ((value & (NodeClass.Object | NodeClass.View)) != 0) {
                        // ensure a valid event filter.
                        if (!(_filter is EventFilter)) {
                            UseDefaultEventFilter();
                        }

                        // set the queue size to the default for events.
                        if (QueueSize <= 1) {
                            QueueSize = int.MaxValue;
                        }

                        _eventCache = new MonitoredItemEventCache(100);
                        AttributeId = Attributes.EventNotifier;
                    }
                    else {
                        // clear the filter if it is only valid for events.
                        if (_filter is EventFilter) {
                            _filter = null;
                        }

                        // set the queue size to the default for data changes.
                        if (QueueSize == int.MaxValue) {
                            QueueSize = 1;
                        }

                        _dataCache = new MonitoredItemDataCache(1);
                    }
                }

                _nodeClass = value;
            }
        }

        /// <summary>
        /// The attribute to monitor.
        /// </summary>
        [DataMember(Order = 5)]
        public uint AttributeId { get; set; }

        /// <summary>
        /// The range of array indexes to monitor.
        /// </summary>
        [DataMember(Order = 6)]
        public string IndexRange { get; set; }

        /// <summary>
        /// The encoding to use when returning notifications.
        /// </summary>
        [DataMember(Order = 7)]
        public QualifiedName Encoding { get; set; }

        /// <summary>
        /// The monitoring mode.
        /// </summary>
        [DataMember(Order = 8)]
        public MonitoringMode MonitoringMode { get; set; }

        /// <summary>
        /// The sampling interval.
        /// </summary>
        [DataMember(Order = 9)]
        public int SamplingInterval {
            get => _samplingInterval;

            set {
                if (_samplingInterval != value) {
                    AttributesModified = true;
                }

                _samplingInterval = value;
            }
        }

        /// <summary>
        /// The filter to use to select values to return.
        /// </summary>
        [DataMember(Order = 10)]
        public MonitoringFilter Filter {
            get => _filter;

            set {
                // validate filter against node class.
                ValidateFilter(_nodeClass, value);

                AttributesModified = true;
                _filter = value;
            }
        }

        /// <summary>
        /// The length of the queue used to buffer values.
        /// </summary>
        [DataMember(Order = 11)]
        public uint QueueSize {
            get => _queueSize;

            set {
                if (_queueSize != value) {
                    AttributesModified = true;
                }

                _queueSize = value;
            }
        }

        /// <summary>
        /// Whether to discard the oldest entries in the queue when it is full.
        /// </summary>
        [DataMember(Order = 12)]
        public bool DiscardOldest {
            get => _discardOldest;

            set {
                if (_discardOldest != value) {
                    AttributesModified = true;
                }

                _discardOldest = value;
            }
        }
        #endregion

        #region Dynamic Properties
        /// <summary>
        /// The subscription that owns the monitored item.
        /// </summary>
        public Subscription Subscription { get; internal set; }

        /// <summary>
        /// A local handle assigned to the monitored item.
        /// </summary>
        public object Handle { get; set; }

        /// <summary>
        /// Whether the item has been created on the server.
        /// </summary>
        public bool Created => Status.Created;

        /// <summary>
        /// The identifier assigned by the client.
        /// </summary>
        public uint ClientHandle { get; private set; }

        /// <summary>
        /// The node id to monitor after applying any relative path.
        /// </summary>
        public NodeId ResolvedNodeId {
            get {
                // just return the start id if relative path is empty.
                if (string.IsNullOrEmpty(_relativePath)) {
                    return StartNodeId;
                }

                return _resolvedNodeId;
            }

            internal set => _resolvedNodeId = value;
        }

        /// <summary>
        /// Whether the monitoring attributes have been modified since the item was created.
        /// </summary>
        public bool AttributesModified { get; private set; }

        /// <summary>
        /// The status associated with the monitored item.
        /// </summary>
        public MonitoredItemStatus2 Status { get; private set; }
        #endregion

        #region Cache Related Functions
        /// <summary>
        /// Returns the queue size used by the cache.
        /// </summary>
        public int CacheQueueSize {
            get {
                lock (_cache) {
                    if (_dataCache != null) {
                        return _dataCache.QueueSize;
                    }

                    if (_eventCache != null) {
                        return _eventCache.QueueSize;
                    }

                    return 0;
                }
            }

            set {
                lock (_cache) {
                    if (_dataCache != null) {
                        _dataCache.SetQueueSize(value);
                    }

                    if (_eventCache != null) {
                        _eventCache.SetQueueSize(value);
                    }
                }
            }
        }

        /// <summary>
        /// The last value or event received from the server.
        /// </summary>
        public IEncodeable LastValue {
            get {
                lock (_cache) {
                    return _lastNotification;
                }
            }
        }

        /// <summary>
        /// Read all values in the cache queue.
        /// </summary>
        public IList<DataValue> DequeueValues() {
            lock (_cache) {
                if (_dataCache != null) {
                    return _dataCache.Publish();
                }

                return new List<DataValue>();
            }
        }

        /// <summary>
        /// Read all events in the cache queue.
        /// </summary>
        public IList<EventFieldList> DequeueEvents() {
            lock (_cache) {
                if (_eventCache != null) {
                    return _eventCache.Publish();
                }

                return new List<EventFieldList>();
            }
        }

        /// <summary>
        /// The last message containing a notification for the item.
        /// </summary>
        public NotificationMessage LastMessage {
            get {
                lock (_cache) {
                    if (_dataCache != null) {
                        return ((MonitoredItemNotification)_lastNotification).Message;
                    }

                    if (_eventCache != null) {
                        return ((EventFieldList)_lastNotification).Message;
                    }

                    return null;
                }
            }
        }

        /// <summary>
        /// Raised when a new notification arrives.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1009:DeclareEventHandlersCorrectly")]
        public event MonitoredItemNotificationEventHandler Notification {
            add {
                lock (_cache) {
                    _Notification += value;
                }
            }

            remove {
                lock (_cache) {
                    _Notification -= value;
                }
            }
        }

        /// <summary>
        /// Saves a data change or event in the cache.
        /// </summary>
        public void SaveValueInCache(IEncodeable newValue) {
            lock (_cache) {
                _lastNotification = newValue;

                if (_dataCache != null) {
                    if (newValue is MonitoredItemNotification datachange) {
                        // validate the ServerTimestamp of the notification.
                        if (datachange.Value != null && datachange.Value.ServerTimestamp > DateTime.UtcNow) {
                            Opc.Ua.Utils.Trace("Received ServerTimestamp {0} is in the future for MonitoredItemId {1}", datachange.Value.ServerTimestamp.ToLocalTime(), ClientHandle);
                        }

                        // validate SourceTimestamp of the notification.
                        if (datachange.Value != null && datachange.Value.SourceTimestamp > DateTime.UtcNow) {
                            Opc.Ua.Utils.Trace("Received SourceTimestamp {0} is in the future for MonitoredItemId {1}", datachange.Value.SourceTimestamp.ToLocalTime(), ClientHandle);
                        }

                        if (datachange.Value != null && datachange.Value.StatusCode.Overflow) {
                            Opc.Ua.Utils.Trace("Overflow bit set for data change with ServerTimestamp {0} and value {1} for MonitoredItemId {2}", datachange.Value.ServerTimestamp.ToLocalTime(), datachange.Value.Value, ClientHandle);
                        }

                        _dataCache.OnNotification(datachange);
                    }
                }

                if (_eventCache != null) {
                    var eventchange = newValue as EventFieldList;

                    if (_eventCache != null) {
                        _eventCache.OnNotification(eventchange);
                    }
                }

                _Notification?.Invoke(this, new MonitoredItemNotificationEventArgs(newValue));
            }
        }
        #endregion

        #region ICloneable Members
        /// <summary>
        /// Creates a deep copy of the object.
        /// </summary>
        public new object MemberwiseClone() {
            return new MonitoredItem(this);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Sets the error status for the monitored item.
        /// </summary>
        public void SetError(ServiceResult error) {
            Status.SetError(error);
        }

        /// <summary>
        /// Updates the object with the results of a translate browse path request.
        /// </summary>
        public void SetResolvePathResult(
            BrowsePathResult result,
            int index,
            DiagnosticInfoCollection diagnosticInfos,
            ResponseHeader responseHeader) {
            ServiceResult error = null;

            if (StatusCode.IsBad(result.StatusCode)) {
                error = ClientBase.GetResult(result.StatusCode, index, diagnosticInfos, responseHeader);
            }
            else {
                ResolvedNodeId = NodeId.Null;

                // update the node id.
                if (result.Targets.Count > 0) {
                    ResolvedNodeId = ExpandedNodeId.ToNodeId(result.Targets[0].TargetId, Subscription.Session.NamespaceUris);
                }
            }

            Status.SetResolvePathResult(result, error);
        }

        /// <summary>
        /// Updates the object with the results of a create monitored item request.
        /// </summary>
        public void SetCreateResult(
            MonitoredItemCreateRequest request,
            MonitoredItemCreateResult result,
            int index,
            DiagnosticInfoCollection diagnosticInfos,
            ResponseHeader responseHeader) {
            ServiceResult error = null;

            if (StatusCode.IsBad(result.StatusCode)) {
                error = ClientBase.GetResult(result.StatusCode, index, diagnosticInfos, responseHeader);
            }

            Status.SetCreateResult(request, result, error);
            AttributesModified = false;
        }

        /// <summary>
        /// Updates the object with the results of a modify monitored item request.
        /// </summary>
        public void SetModifyResult(
            MonitoredItemModifyRequest request,
            MonitoredItemModifyResult result,
            int index,
            DiagnosticInfoCollection diagnosticInfos,
            ResponseHeader responseHeader) {
            ServiceResult error = null;

            if (StatusCode.IsBad(result.StatusCode)) {
                error = ClientBase.GetResult(result.StatusCode, index, diagnosticInfos, responseHeader);
            }

            Status.SetModifyResult(request, result, error);
            AttributesModified = false;
        }

        /// <summary>
        /// Updates the object with the results of a modify monitored item request.
        /// </summary>
        public void SetDeleteResult(
            StatusCode result,
            int index,
            DiagnosticInfoCollection diagnosticInfos,
            ResponseHeader responseHeader) {
            ServiceResult error = null;

            if (StatusCode.IsBad(result)) {
                error = ClientBase.GetResult(result, index, diagnosticInfos, responseHeader);
            }

            Status.SetDeleteResult(error);
        }

        /// <summary>
        /// Returns the field name the specified SelectClause in the EventFilter.
        /// </summary>
        public string GetFieldName(int index) {
            if (!(_filter is EventFilter filter)) {
                return null;
            }

            if (index < 0 || index >= filter.SelectClauses.Count) {
                return null;
            }

            return Opc.Ua.Utils.Format("{0}", SimpleAttributeOperand.Format(filter.SelectClauses[index].BrowsePath));
        }

        /// <summary>
        /// Returns value of the field name containing the event type.
        /// </summary>
        public object GetFieldValue(
            EventFieldList eventFields,
            NodeId eventTypeId,
            string browsePath,
            uint attributeId) {
            var browseNames = SimpleAttributeOperand.Parse(browsePath);
            return GetFieldValue(eventFields, eventTypeId, browseNames, attributeId);
        }

        /// <summary>
        /// Returns value of the field name containing the event type.
        /// </summary>
        public object GetFieldValue(
            EventFieldList eventFields,
            NodeId eventTypeId,
            QualifiedName browseName) {
            var browsePath = new QualifiedNameCollection {
                browseName
            };
            return GetFieldValue(eventFields, eventTypeId, browsePath, Attributes.Value);
        }

        /// <summary>
        /// Returns value of the field name containing the event type.
        /// </summary>
        public object GetFieldValue(
            EventFieldList eventFields,
            NodeId eventTypeId,
            IList<QualifiedName> browsePath,
            uint attributeId) {
            if (eventFields == null) {
                return null;
            }


            if (!(_filter is EventFilter filter)) {
                return null;
            }

            for (var ii = 0; ii < filter.SelectClauses.Count; ii++) {
                if (ii >= eventFields.EventFields.Count) {
                    return null;
                }

                // check for match.
                var clause = filter.SelectClauses[ii];

                // attribute id
                if (clause.AttributeId != attributeId) {
                    continue;
                }

                // match null browse path.
                if (browsePath == null || browsePath.Count == 0) {
                    if (clause.BrowsePath != null && clause.BrowsePath.Count > 0) {
                        continue;
                    }

                    // ignore event type id when matching null browse paths.
                    return eventFields.EventFields[ii].Value;
                }

                // match browse path.

                // event type id.
                if (clause.TypeDefinitionId != eventTypeId) {
                    continue;
                }

                // match element count.
                if (clause.BrowsePath.Count != browsePath.Count) {
                    continue;
                }

                // check each element.
                var match = true;

                for (var jj = 0; jj < clause.BrowsePath.Count; jj++) {
                    if (clause.BrowsePath[jj] != browsePath[jj]) {
                        match = false;
                        break;
                    }
                }

                // check of no match.
                if (!match) {
                    continue;
                }

                // return value.
                return eventFields.EventFields[ii].Value;
            }

            // no event type in event field list.
            return null;
        }

        /// <summary>
        /// Returns value of the field name containing the event type.
        /// </summary>
        public INode GetEventType(EventFieldList eventFields) {
            // get event type.
            var eventTypeId = GetFieldValue(eventFields, ObjectTypes.BaseEventType, BrowseNames.EventType) as NodeId;

            if (eventTypeId != null && Subscription != null && Subscription.Session != null) {
                return Subscription.Session.NodeCache.Find(eventTypeId);
            }

            // no event type in event field list.
            return null;
        }

        /// <summary>
        /// Returns value of the field name containing the event type.
        /// </summary>
        public DateTime GetEventTime(EventFieldList eventFields) {
            // get event time.
            var eventTime = GetFieldValue(eventFields, ObjectTypes.BaseEventType, BrowseNames.Time) as DateTime?;

            if (eventTime != null) {
                return eventTime.Value;
            }

            // no event time in event field list.
            return DateTime.MinValue;
        }

        /// <summary>
        /// The service result for a data change notification.
        /// </summary>
        public static ServiceResult GetServiceResult(IEncodeable notification) {
            if (!(notification is MonitoredItemNotification datachange)) {
                return null;
            }

            var message = datachange.Message;

            if (message == null) {
                return null;
            }

            return new ServiceResult(datachange.Value.StatusCode, datachange.DiagnosticInfo, message.StringTable);
        }

        /// <summary>
        /// The service result for a field in an notification (the field must contain a Status object).
        /// </summary>
        public static ServiceResult GetServiceResult(IEncodeable notification, int index) {
            if (!(notification is EventFieldList eventFields)) {
                return null;
            }

            var message = eventFields.Message;

            if (message == null) {
                return null;
            }

            if (index < 0 || index >= eventFields.EventFields.Count) {
                return null;
            }


            if (!(ExtensionObject.ToEncodeable(eventFields.EventFields[index].Value as ExtensionObject) is StatusResult status)) {
                return null;
            }

            return new ServiceResult(status.StatusCode, status.DiagnosticInfo, message.StringTable);
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Throws an exception if the flter cannot be used with the node class.
        /// </summary>
        private void ValidateFilter(NodeClass nodeClass, MonitoringFilter filter) {
            if (filter == null) {
                return;
            }

            switch (nodeClass) {
                case NodeClass.Variable:
                case NodeClass.VariableType: {
                        if (!typeof(DataChangeFilter).IsInstanceOfType(filter)) {
                            _nodeClass = NodeClass.Variable;
                        }

                        break;
                    }

                case NodeClass.Object:
                case NodeClass.View: {
                        if (!typeof(EventFilter).IsInstanceOfType(filter)) {
                            _nodeClass = NodeClass.Object;
                        }

                        break;
                    }

                default: {
                        throw ServiceResultException.Create(StatusCodes.BadFilterNotAllowed, "Filters may not be specified for nodes of class '{0}'.", nodeClass);
                    }
            }
        }

        /// <summary>
        /// Sets the default event filter.
        /// </summary>
        private void UseDefaultEventFilter() {
            EventFilter filter = filter = new EventFilter();

            filter.AddSelectClause(ObjectTypes.BaseEventType, BrowseNames.EventId);
            filter.AddSelectClause(ObjectTypes.BaseEventType, BrowseNames.EventType);
            filter.AddSelectClause(ObjectTypes.BaseEventType, BrowseNames.SourceNode);
            filter.AddSelectClause(ObjectTypes.BaseEventType, BrowseNames.SourceName);
            filter.AddSelectClause(ObjectTypes.BaseEventType, BrowseNames.Time);
            filter.AddSelectClause(ObjectTypes.BaseEventType, BrowseNames.ReceiveTime);
            filter.AddSelectClause(ObjectTypes.BaseEventType, BrowseNames.LocalTime);
            filter.AddSelectClause(ObjectTypes.BaseEventType, BrowseNames.Message);
            filter.AddSelectClause(ObjectTypes.BaseEventType, BrowseNames.Severity);

            _filter = filter;
        }
        #endregion

        #region Private Fields
        private string _relativePath;
        private NodeId _resolvedNodeId;
        private NodeClass _nodeClass;
        private int _samplingInterval;
        private MonitoringFilter _filter;
        private uint _queueSize;
        private bool _discardOldest;
        private static long s_GlobalClientHandle;

        private object _cache = new object();
        private MonitoredItemDataCache _dataCache;
        private MonitoredItemEventCache _eventCache;
        private IEncodeable _lastNotification;
        private event MonitoredItemNotificationEventHandler _Notification;
        #endregion
    }

    #region MonitoredItemEventArgs Class
    /// <summary>
    /// The event arguments provided when a new notification message arrives.
    /// </summary>
    public class MonitoredItemNotificationEventArgs : EventArgs {
        #region Constructors
        /// <summary>
        /// Creates a new instance.
        /// </summary>
        internal MonitoredItemNotificationEventArgs(IEncodeable notificationValue) {
            NotificationValue = notificationValue;
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// The new notification.
        /// </summary>
        public IEncodeable NotificationValue { get; }
        #endregion

        #region Private Fields
        #endregion
    }

    /// <summary>
    /// The delegate used to receive monitored item value notifications.
    /// </summary>
    public delegate void MonitoredItemNotificationEventHandler(MonitoredItem monitoredItem, MonitoredItemNotificationEventArgs e);
    #endregion

    /// <summary>
    /// An item in the cache
    /// </summary>
    public class MonitoredItemDataCache {
        #region Constructors
        /// <summary>
        /// Constructs a cache for a monitored item.
        /// </summary>
        public MonitoredItemDataCache(int queueSize) {
            QueueSize = queueSize;
            _values = new Queue<DataValue>();
        }
        #endregion

        #region Public Members
        /// <summary>
        /// The size of the queue to maintain.
        /// </summary>
        public int QueueSize { get; private set; }

        /// <summary>
        /// The last value received from the server.
        /// </summary>
        public DataValue LastValue { get; private set; }

        /// <summary>
        /// Returns all values in the queue.
        /// </summary>
        public IList<DataValue> Publish() {
            var values = new DataValue[_values.Count];

            for (var ii = 0; ii < values.Length; ii++) {
                values[ii] = _values.Dequeue();
            }

            return values;
        }

        /// <summary>
        /// Saves a notification in the cache.
        /// </summary>
        public void OnNotification(MonitoredItemNotification notification) {
            _values.Enqueue(notification.Value);
            LastValue = notification.Value;

            Opc.Ua.Utils.Trace(
                "NotificationReceived: ClientHandle={0}, Value={1}",
                notification.ClientHandle,
                LastValue.Value);

            while (_values.Count > QueueSize) {
                _values.Dequeue();
            }
        }

        /// <summary>
        /// Changes the queue size.
        /// </summary>
        public void SetQueueSize(int queueSize) {
            if (queueSize == QueueSize) {
                return;
            }

            if (queueSize < 1) {
                queueSize = 1;
            }

            QueueSize = queueSize;

            while (_values.Count > QueueSize) {
                _values.Dequeue();
            }
        }
        #endregion

        #region Private Fields
        private readonly Queue<DataValue> _values;
        #endregion
    }

    /// <summary>
    /// Saves the events received from the srever.
    /// </summary>
    public class MonitoredItemEventCache {
        #region Constructors
        /// <summary>
        /// Constructs a cache for a monitored item.
        /// </summary>
        public MonitoredItemEventCache(int queueSize) {
            QueueSize = queueSize;
            _events = new Queue<EventFieldList>();
        }
        #endregion

        #region Public Members
        /// <summary>
        /// The size of the queue to maintain.
        /// </summary>
        public int QueueSize { get; private set; }

        /// <summary>
        /// The last event received.
        /// </summary>
        public EventFieldList LastEvent { get; private set; }

        /// <summary>
        /// Returns all events in the queue.
        /// </summary>
        public IList<EventFieldList> Publish() {
            var events = new EventFieldList[_events.Count];

            for (var ii = 0; ii < events.Length; ii++) {
                events[ii] = _events.Dequeue();
            }

            return events;
        }

        /// <summary>
        /// Saves a notification in the cache.
        /// </summary>
        public void OnNotification(EventFieldList notification) {
            _events.Enqueue(notification);
            LastEvent = notification;

            while (_events.Count > QueueSize) {
                _events.Dequeue();
            }
        }

        /// <summary>
        /// Changes the queue size.
        /// </summary>
        public void SetQueueSize(int queueSize) {
            if (queueSize == QueueSize) {
                return;
            }

            if (queueSize < 1) {
                queueSize = 1;
            }

            QueueSize = queueSize;

            while (_events.Count > QueueSize) {
                _events.Dequeue();
            }
        }
        #endregion

        #region Private Fields
        private readonly Queue<EventFieldList> _events;
        #endregion
    }
}
