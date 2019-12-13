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
    using System.Globalization;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Manages a session with a server.
    /// </summary>
    public class Session : SessionClient, IDisposable {

        /// <summary>
        /// Constructs a new instance of the session.
        /// </summary>
        /// <param name="channel">The channel used to communicate with the server.</param>
        /// <param name="configuration">The configuration for the client application.</param>
        /// <param name="endpoint">The endpoint use to initialize the channel.</param>
        /// <param name="clientCertificate">The certificate to use for the client.</param>
        /// <remarks>
        /// The application configuration is used to look up the certificate if none is provided.
        /// The clientCertificate must have the private key. This will require that the certificate
        /// be loaded from a certicate store. Converting a DER encoded blob to a X509Certificate2
        /// will not include a private key.
        /// </remarks>
        public Session(
            ITransportChannel channel,
            ApplicationConfiguration configuration,
            ConfiguredEndpoint endpoint,
            X509Certificate2 clientCertificate)
        :
            base(channel) {
            Initialize(channel, configuration, endpoint, clientCertificate);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="configuration"></param>
        /// <param name="endpoint"></param>
        /// <param name="clientCertificate"></param>
        /// <param name="availableEndpoints"></param>
        public Session(
            ITransportChannel channel,
            ApplicationConfiguration configuration,
            ConfiguredEndpoint endpoint,
            X509Certificate2 clientCertificate,
            EndpointDescriptionCollection availableEndpoints)
            :
                base(channel) {
            Initialize(channel, configuration, endpoint, clientCertificate);

            _expectedServerEndpoints = availableEndpoints;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Session"/> class.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <param name="template">The template session.</param>
        /// <param name="copyEventHandlers">if set to <c>true</c> the event handlers are copied.</param>
        public Session(ITransportChannel channel, Session template, bool copyEventHandlers)
        :
            base(channel) {
            Initialize(channel, template._configuration, template.ConfiguredEndpoint, template.InstanceCertificate);

            DefaultSubscription = template.DefaultSubscription;
            _sessionTimeout = template._sessionTimeout;
            _maxRequestMessageSize = template._maxRequestMessageSize;
            PreferredLocales = template.PreferredLocales;
            SessionName = template.SessionName;
            Handle = template.Handle;
            Identity = template.Identity;
            _keepAliveInterval = template._keepAliveInterval;
            _checkDomain = template._checkDomain;

            if (copyEventHandlers) {
                _PublishError = template._PublishError;
                _SubscriptionsChanged = template._SubscriptionsChanged;
                _SessionClosing = template._SessionClosing;
            }

            foreach (var subscription in template.Subscriptions) {
                AddSubscription(new Subscription(subscription, copyEventHandlers));
            }
        }

        /// <summary>
        /// Initializes the channel.
        /// </summary>
        private void Initialize(
            ITransportChannel channel,
            ApplicationConfiguration configuration,
            ConfiguredEndpoint endpoint,
            X509Certificate2 clientCertificate) {
            Initialize();

            // save configuration information.
            _configuration = configuration;
            ConfiguredEndpoint = endpoint;

            // update the default subscription.
            DefaultSubscription.MinLifetimeInterval = (uint)configuration.ClientConfiguration.MinSubscriptionLifetime;

            if (ConfiguredEndpoint.Description.SecurityPolicyUri != SecurityPolicies.None) {
                // update client certificate.
                InstanceCertificate = clientCertificate;

                if (clientCertificate == null) {
                    // load the application instance certificate.
                    if (_configuration.SecurityConfiguration.ApplicationCertificate == null) {
                        throw new ServiceResultException(
                            StatusCodes.BadConfigurationError,
                            "The client configuration does not specify an application instance certificate.");
                    }

                    InstanceCertificate = _configuration.SecurityConfiguration.ApplicationCertificate.Find(true).Result;
                }

                // check for valid certificate.
                if (InstanceCertificate == null) {
                    throw ServiceResultException.Create(
                        StatusCodes.BadConfigurationError,
                        "Cannot find the application instance certificate. Store={0}, SubjectName={1}, Thumbprint={2}.",
                        _configuration.SecurityConfiguration.ApplicationCertificate.StorePath,
                        _configuration.SecurityConfiguration.ApplicationCertificate.SubjectName,
                        _configuration.SecurityConfiguration.ApplicationCertificate.Thumbprint);
                }

                // check for private key.
                if (!InstanceCertificate.HasPrivateKey) {
                    throw ServiceResultException.Create(
                        StatusCodes.BadConfigurationError,
                        "Do not have a privat key for the application instance certificate. Subject={0}, Thumbprint={1}.",
                        InstanceCertificate.Subject,
                        InstanceCertificate.Thumbprint);
                }

                // load certificate chain.
                _instanceCertificateChain = new X509Certificate2Collection(InstanceCertificate);
                var issuers = new List<CertificateIdentifier>();
                configuration.CertificateValidator.GetIssuers(InstanceCertificate, issuers).Wait();

                for (var i = 0; i < issuers.Count; i++) {
                    _instanceCertificateChain.Add(issuers[i].Certificate);
                }
            }

            // initialize the message context.
            var messageContext = channel.MessageContext;

            if (messageContext != null) {
                NamespaceUris = messageContext.NamespaceUris;
                ServerUris = messageContext.ServerUris;
                Factory = messageContext.Factory;
            }
            else {
                NamespaceUris = new NamespaceTable();
                ServerUris = new StringTable();
                Factory = ServiceMessageContext.GlobalContext.Factory;
            }

            // set the default preferred locales.
            PreferredLocales = new string[] { CultureInfo.CurrentCulture.Name };

            // create a context to use.
            _systemContext = new SystemContext {
                SystemHandle = this,
                EncodeableFactory = Factory,
                NamespaceUris = NamespaceUris,
                ServerUris = ServerUris,
                TypeTable = TypeTree,
                PreferredLocales = null,
                SessionId = null,
                UserIdentity = null
            };
        }

        /// <summary>
        /// Sets the object members to default values.
        /// </summary>
        private void Initialize() {
            _sessionTimeout = 0;
            NamespaceUris = new NamespaceTable();
            ServerUris = new StringTable();
            Factory = EncodeableFactory.GlobalFactory;
            NodeCache = new Opc.Ua.Client.NodeCache(this);
            _configuration = null;
            InstanceCertificate = null;
            ConfiguredEndpoint = null;
            _subscriptions = new List<Subscription>();
            _dictionaries = new Dictionary<NodeId, Opc.Ua.Client.DataDictionary>();
            _acknowledgementsToSend = new SubscriptionAcknowledgementCollection();
            _latestAcknowledgementsSent = new Dictionary<uint, uint>();
            _identityHistory = new List<IUserIdentity>();
            _outstandingRequests = new LinkedList<AsyncRequestState>();
            _keepAliveInterval = 5000;
            SessionName = "";

            DefaultSubscription = new Subscription {
                DisplayName = "Subscription",
                PublishingInterval = 1000,
                KeepAliveCount = 10,
                LifetimeCount = 1000,
                Priority = 255,
                PublishingEnabled = true
            };
        }



        /// <summary>
        /// Closes the session and the underlying channel.
        /// </summary>
        protected override void Dispose(bool disposing) {
            if (disposing) {
                Utils.SilentDispose(_keepAliveTimer);
                _keepAliveTimer = null;

                foreach (var subscription in _subscriptions) {
                    Utils.SilentDispose(subscription);
                }

                _subscriptions.Clear();
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Gets the subscriptions owned by the session.
        /// </summary>
        public IEnumerable<Subscription> Subscriptions {
            get {
                lock (SyncRoot) {
                    return new ReadOnlyList<Subscription>(_subscriptions);
                }
            }
        }

        /// <summary>
        /// Call on recreate
        /// </summary>
        /// <param name="session"></param>
        public static void OnRecreate(Session session) {

            // create the subscriptions.
            foreach (var subscription in session.Subscriptions) {
                subscription.Create();
            }
        }

        /// <summary>
        /// Call on reconnect
        /// </summary>
        public void OnReconnect(Session session) {
            var publishCount = 0;
            lock (SyncRoot) {
                publishCount = _subscriptions.Count;
            }

            // refill pipeline.
            for (var ii = 0; ii < publishCount; ii++) {

                // Push publish operations

                BeginPublish(OperationTimeout);
            }
        }

        /// <summary>
        /// Adds a subscription to the session.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="subscription">The subscription to add.</param>
        /// <returns></returns>
        public bool AddSubscription(Session session, Subscription subscription) {
            if (subscription == null) {
                throw new ArgumentNullException("subscription");
            }
            lock (SyncRoot) {
                if (_subscriptions.Contains(subscription)) {
                    return false;
                }
                subscription.Session = session;
                _subscriptions.Add(subscription);
            }

            _SubscriptionsChanged?.Invoke(this, null);
            return true;
        }

        /// <summary>
        /// Removes a subscription from the session.
        /// </summary>
        /// <param name="subscription">The subscription to remove.</param>
        /// <returns></returns>
        public bool RemoveSubscription(Subscription subscription) {
            if (subscription == null) {
                throw new ArgumentNullException("subscription");
            }

            if (subscription.Created) {
                subscription.DeleteAsync(false);
            }

            lock (SyncRoot) {
                if (!_subscriptions.Remove(subscription)) {
                    return false;
                }

                subscription.Session = null;
            }

            _SubscriptionsChanged?.Invoke(this, null);
            return true;
        }












        /// <summary>
        /// Sends an additional publish request.
        /// </summary>
        public async Task PublishAsync(Session session, int timeout) {

            while (true) {

                // TODO : ExecuteAsync(...

                // do not publish if reconnecting.
                if (_reconnecting) {
                    Utils.Trace("Published skipped due to reconnect");
                    return;
                }

                SubscriptionAcknowledgementCollection acknowledgementsToSend = null;

                // collect the current set if acknowledgements.
                lock (SyncRoot) {
                    acknowledgementsToSend = _acknowledgementsToSend;
                    _acknowledgementsToSend = new SubscriptionAcknowledgementCollection();
                    foreach (var toSend in acknowledgementsToSend) {
                        if (_latestAcknowledgementsSent.ContainsKey(toSend.SubscriptionId)) {
                            _latestAcknowledgementsSent[toSend.SubscriptionId] = toSend.SequenceNumber;
                        }
                        else {
                            _latestAcknowledgementsSent.Add(toSend.SubscriptionId, toSend.SequenceNumber);
                        }
                    }
                }

                // send publish request.
                var requestHeader = new RequestHeader {
                    // ensure the publish request is discarded before the timeout occurs to ensure the channel is dropped.
                    TimeoutHint = (uint)OperationTimeout / 2,
                    ReturnDiagnostics = (uint)(int)ReturnDiagnostics,
                    RequestHandle = Utils.IncrementIdentifier(ref _publishCounter)
                };

                try {
                    var response = await session.PublishAsync(requestHeader,
                        acknowledgementsToSend);

                    // extract state information.
                    var moreNotifications = response.MoreNotifications;

                    // complete publish.
                    var subscriptionId = response.SubscriptionId;
                    var availableSequenceNumbers = response.AvailableSequenceNumbers;
                    var notificationMessage = response.NotificationMessage;
                    var acknowledgeResults = response.Results;
                    var acknowledgeDiagnosticInfos = response.DiagnosticInfos;

                    foreach (var code in acknowledgeResults) {
                        if (StatusCode.IsBad(code)) {
                            Utils.Trace("Error - Publish call finished. ResultCode={0}; SubscriptionId={1};", code.ToString(), subscriptionId);
                        }
                    }

                    Utils.Trace("NOTIFICATION RECEIVED: SubId={0}, SeqNo={1}",
                        subscriptionId, notificationMessage.SequenceNumber);

                    // process response.
                    ProcessPublishResponse(
                        response.ResponseHeader,
                        subscriptionId,
                        availableSequenceNumbers,
                        moreNotifications,
                        notificationMessage);

                    // nothing more to do if reconnecting.
                    if (_reconnecting) {
                        Utils.Trace("No new publish sent because of reconnect in progress.");
                        return;
                    }
                }
                catch (Exception e) {
                    if (_subscriptions.Count == 0) {
                        // Publish responses with error should occur after deleting the last subscription.
                        Utils.Trace("Publish #{0}, Subscription count = 0, Error: {1}", requestHeader.RequestHandle, e.Message);
                    }
                    else {
                        Utils.Trace("Publish #{0}, Reconnecting={2}, Error: {1}", requestHeader.RequestHandle, e.Message, _reconnecting);
                    }

                    // ignore errors if reconnecting.
                    if (_reconnecting) {
                        Utils.Trace("Publish abandoned after error due to reconnect: {0}", e.Message);
                        return;
                    }

                    // try to acknowledge the notifications again in the next publish.
                    if (acknowledgementsToSend != null) {
                        lock (SyncRoot) {
                            _acknowledgementsToSend.AddRange(acknowledgementsToSend);
                        }
                    }

                    // raise an error event.
                    var error = new ServiceResult(e);

                    if (error.Code != StatusCodes.BadNoSubscription) {
                        PublishErrorEventHandler callback = null;

                        lock (_eventLock) {
                            callback = _PublishError;
                        }

                        if (callback != null) {
                            try {
                                callback(this, new PublishErrorEventArgs(error));
                            }
                            catch (Exception e2) {
                                Utils.Trace(e2, "Session: Unexpected error invoking PublishErrorCallback.");
                            }
                        }
                    }

                    // don't send another publish for these errors.
                    switch (error.Code) {
                        case StatusCodes.BadNoSubscription:
                        case StatusCodes.BadSessionClosed:
                        case StatusCodes.BadSessionIdInvalid:
                        case StatusCodes.BadTooManyPublishRequests:
                        case StatusCodes.BadServerHalted:
                            return;
                    }
                    Utils.Trace(e, "PUBLISH #{0} - Unhandled error during Publish.", requestHeader.RequestHandle);
                }

                var requestCount = GoodPublishRequestCount;

                if (requestCount >= _subscriptions.Count) {
                    Utils.Trace("PUBLISH - Did not send another publish request. GoodPublishRequestCount={0}, Subscriptions={1}", requestCount, _subscriptions.Count);
                    return;
                }
            }
        }

        /// <summary>
        /// Sends a republish request.
        /// </summary>
        public async Task<bool> RepublishAsync(Session session,
            uint subscriptionId, uint sequenceNumber) {

            try {
                Utils.Trace("Requesting Republish for {0}-{1}", subscriptionId, sequenceNumber);
                // send publish request.
                var response = await session.RepublishAsync(
                    new RequestHeader {
                        TimeoutHint = (uint)OperationTimeout,
                        ReturnDiagnostics = (uint)(int)ReturnDiagnostics,
                        RequestHandle = Utils.IncrementIdentifier(ref _publishCounter)
                    }, subscriptionId, sequenceNumber);


                Utils.Trace("Received Republish for {0}-{1}", subscriptionId, sequenceNumber);

                // process response.
                ProcessPublishResponse(response.ResponseHeader, subscriptionId,
                    null, false, response.NotificationMessage);
                return true;
            }
            catch (Exception e) {
                var error = new ServiceResult(e);

                var result = error.StatusCode == StatusCodes.BadMessageNotAvailable;

                if (result) {
                    Utils.Trace("Message {0}-{1} no longer available.", subscriptionId, sequenceNumber);
                }
                else {
                    Utils.Trace(e, "Unexpected error sending republish request.");
                }

                PublishErrorEventHandler callback = null;

                lock (_eventLock) {
                    callback = _PublishError;
                }

                // raise an error event.
                if (callback != null) {
                    try {
                        var args = new PublishErrorEventArgs(error, subscriptionId, sequenceNumber);
                        callback(this, args);
                    }
                    catch (Exception e2) {
                        Utils.Trace(e2, "Session: Unexpected error invoking PublishErrorCallback.");
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// Processes the response from a publish request.
        /// </summary>
        private void ProcessPublishResponse(
            ResponseHeader responseHeader,
            uint subscriptionId,
            UInt32Collection availableSequenceNumbers,
            bool moreNotifications,
            NotificationMessage notificationMessage) {
            Subscription subscription = null;


            // collect the current set if acknowledgements.
            lock (SyncRoot) {
                // clear out acknowledgements for messages that the server does not have any more.
                var acknowledgementsToSend = new SubscriptionAcknowledgementCollection();

                for (var ii = 0; ii < _acknowledgementsToSend.Count; ii++) {
                    var acknowledgement = _acknowledgementsToSend[ii];

                    if (acknowledgement.SubscriptionId != subscriptionId) {
                        acknowledgementsToSend.Add(acknowledgement);
                    }
                    else {
                        if (availableSequenceNumbers == null || availableSequenceNumbers.Contains(acknowledgement.SequenceNumber)) {
                            acknowledgementsToSend.Add(acknowledgement);
                        }
                    }
                }

                // create an acknowledgement to be sent back to the server.
                if (notificationMessage.NotificationData.Count > 0) {
                    var acknowledgement = new SubscriptionAcknowledgement {
                        SubscriptionId = subscriptionId,
                        SequenceNumber = notificationMessage.SequenceNumber
                    };

                    acknowledgementsToSend.Add(acknowledgement);
                }

                uint lastSentSequenceNumber = 0;
                if (availableSequenceNumbers != null) {
                    foreach (var availableSequenceNumber in availableSequenceNumbers) {
                        if (_latestAcknowledgementsSent.ContainsKey(subscriptionId)) {
                            lastSentSequenceNumber = _latestAcknowledgementsSent[subscriptionId];

                            // If the last sent sequence number is uint.Max do not display the warning; the counter rolled over
                            // If the last sent sequence number is greater or equal to the available sequence number (returned by the publish), a warning must be logged.
                            if (((lastSentSequenceNumber >= availableSequenceNumber) && (lastSentSequenceNumber != uint.MaxValue)) || (lastSentSequenceNumber == availableSequenceNumber) && (lastSentSequenceNumber == uint.MaxValue)) {
                                Utils.Trace("Received sequence number which was already acknowledged={0}", availableSequenceNumber);
                            }
                        }
                    }
                }

                if (_latestAcknowledgementsSent.ContainsKey(subscriptionId)) {
                    lastSentSequenceNumber = _latestAcknowledgementsSent[subscriptionId];

                    // If the last sent sequence number is uint.Max do not display the warning; the counter rolled over
                    // If the last sent sequence number is greater or equal to the notificationMessage's sequence number (returned by the publish), a warning must be logged.
                    if (((lastSentSequenceNumber >= notificationMessage.SequenceNumber) && (lastSentSequenceNumber != uint.MaxValue)) || (lastSentSequenceNumber == notificationMessage.SequenceNumber) && (lastSentSequenceNumber == uint.MaxValue)) {
                        Utils.Trace("Received sequence number which was already acknowledged={0}", notificationMessage.SequenceNumber);
                    }
                }

                if (availableSequenceNumbers != null) {
                    foreach (var acknowledgement in acknowledgementsToSend) {
                        if (acknowledgement.SubscriptionId == subscriptionId && !availableSequenceNumbers.Contains(acknowledgement.SequenceNumber)) {
                            Utils.Trace("Sequence number={0} was not received in the available sequence numbers.", acknowledgement.SequenceNumber);
                        }
                    }
                }

                _acknowledgementsToSend = acknowledgementsToSend;

                if (notificationMessage.IsEmpty) {
                    Utils.Trace("Empty notification message received for SessionId {0} with PublishTime {1}", SessionId, notificationMessage.PublishTime.ToLocalTime());
                }

                // find the subscription.
                foreach (var current in _subscriptions) {
                    if (current.Id == subscriptionId) {
                        subscription = current;
                        break;
                    }
                }
            }

            // ignore messages with a subscription that has been deleted.
            if (subscription != null) {
                // Validate publish time and reject old values.
                if (notificationMessage.PublishTime.AddMilliseconds(subscription.CurrentPublishingInterval * subscription.CurrentLifetimeCount) < DateTime.UtcNow) {
                    Utils.Trace("PublishTime {0} in publish response is too old for SubscriptionId {1}.", notificationMessage.PublishTime.ToLocalTime(), subscription.Id);
                }

                // Validate publish time and reject old values.
                if (notificationMessage.PublishTime > DateTime.UtcNow.AddMilliseconds(subscription.CurrentPublishingInterval * subscription.CurrentLifetimeCount)) {
                    Utils.Trace("PublishTime {0} in publish response is newer than actual time for SubscriptionId {1}.", notificationMessage.PublishTime.ToLocalTime(), subscription.Id);
                }

                // update subscription cache.
                subscription.SaveMessageInCache(
                    availableSequenceNumbers,
                    notificationMessage,
                    responseHeader.StringTable);

                // raise the notification.
                lock (_eventLock) {
                    var args = new NotificationEventArgs(subscription, notificationMessage, responseHeader.StringTable);
                    if (_Publish != null) {
                        Task.Run(() => {
                            OnRaisePublishNotification(args);
                        });
                    }
                }
            }
            else {
                Utils.Trace("Received Publish Response for Unknown SubscriptionId={0}", subscriptionId);
            }
        }

        /// <summary>
        /// Raises an event indicating that publish has returned a notification.
        /// </summary>
        private void OnRaisePublishNotification(object state) {
            try {
                var args = (NotificationEventArgs)state;
                var callback = _Publish;

                if (callback != null && args.Subscription.Id != 0) {
                    callback(this, args);
                }
            }
            catch (Exception e) {
                Utils.Trace(e, "Session: Unexpected rrror while raising Notification event.");
            }
        }

        private SubscriptionAcknowledgementCollection _acknowledgementsToSend;
        private Dictionary<uint, uint> _latestAcknowledgementsSent;
        private List<Subscription> _subscriptions;
        private Dictionary<NodeId, Opc.Ua.Client.DataDictionary> _dictionaries;
        private double _sessionTimeout;
        private uint _maxRequestMessageSize;
        private SystemContext _systemContext;
        private ApplicationConfiguration _configuration;
        private X509Certificate2Collection _instanceCertificateChain;
        private bool _checkDomain;
        private List<IUserIdentity> _identityHistory;
        private byte[] _serverNonce;
        private X509Certificate2 _serverCertificate;
        private long _publishCounter;
        private int _keepAliveInterval;
        private Timer _keepAliveTimer;
        private bool _reconnecting;

        private readonly EndpointDescriptionCollection _expectedServerEndpoints;

        private readonly object _eventLock = new object();
        private event PublishErrorEventHandler _PublishError;
        private event EventHandler _SubscriptionsChanged;
        private event EventHandler _SessionClosing;
    }

    /// <summary>
    /// Represents the event arguments provided when a publish error occurs.
    /// </summary>
    public class PublishErrorEventArgs : EventArgs {
        /// <summary>
        /// Creates a new instance.
        /// </summary>
        internal PublishErrorEventArgs(ServiceResult status) {
            Status = status;
        }

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        internal PublishErrorEventArgs(ServiceResult status, uint subscriptionId, uint sequenceNumber) {
            Status = status;
            SubscriptionId = subscriptionId;
            SequenceNumber = sequenceNumber;
        }

        /// <summary>
        /// Gets the status associated with the keep alive operation.
        /// </summary>
        public ServiceResult Status { get; }

        /// <summary>
        /// Gets the subscription with the message that could not be republished.
        /// </summary>
        public uint SubscriptionId { get; }

        /// <summary>
        /// Gets the sequence number for the message that could not be republished.
        /// </summary>
        public uint SequenceNumber { get; }
    }

    /// <summary>
    /// The delegate used to receive pubish error notifications.
    /// </summary>
    public delegate void PublishErrorEventHandler(Session session, PublishErrorEventArgs e);
}
