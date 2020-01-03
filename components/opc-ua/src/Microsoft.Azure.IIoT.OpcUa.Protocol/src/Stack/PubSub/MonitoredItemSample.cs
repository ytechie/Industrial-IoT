// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.PubSub {
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Encodeable monitored item message
    /// </summary>
    public class MonitoredItemSample : IEncodeable {

        /// <summary>
        /// Content mask
        /// </summary>
        public uint MessageContentMask { get; set; }

        /// <summary>
        /// Node id
        /// </summary>
        public NodeId NodeId { get; set; }

        /// <summary>
        /// Subscription Id
        /// </summary>
        public string SubscriptionId { get; set; }

        /// <summary>
        /// Endpoint url
        /// </summary>
        public string EndpointUrl { get; set; }

        /// <summary>
        /// Application uri
        /// </summary>
        public string ApplicationUri { get; set; }

        /// <summary>
        /// Display name
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Data value
        /// </summary>
        public DataValue Value { get; set; }

        /// <summary>
        /// Extra fields
        /// </summary>
        public Dictionary<string, string> ExtensionFields { get; set; }

        /// <inheritdoc/>
        public ExpandedNodeId TypeId => ExpandedNodeId.Null;

        /// <inheritdoc/>
        public ExpandedNodeId BinaryEncodingId => ExpandedNodeId.Null;

        /// <inheritdoc/>
        public ExpandedNodeId XmlEncodingId => ExpandedNodeId.Null;

        /// <inheritdoc/>
        public void Decode(IDecoder decoder) {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public void Encode(IEncoder encoder) {
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.NodeId) != 0) {
                encoder.WriteNodeId(nameof(MonitoredItemMessageContentMask.NodeId), NodeId);
            }
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.ServerTimestamp) != 0) {
                encoder.WriteDateTime(nameof(MonitoredItemMessageContentMask.ServerTimestamp), Value.ServerTimestamp);
            }
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.ServerPicoSeconds) != 0) {
                encoder.WriteUInt16(nameof(MonitoredItemMessageContentMask.ServerPicoSeconds), Value.ServerPicoseconds);
            }
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.SourceTimestamp) != 0) {
                encoder.WriteDateTime(nameof(MonitoredItemMessageContentMask.SourceTimestamp), Value.SourceTimestamp);
            }
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.SourcePicoSeconds) != 0) {
                encoder.WriteUInt16(nameof(MonitoredItemMessageContentMask.SourcePicoSeconds), Value.SourcePicoseconds);
            }
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.StatusCode) != 0) {
                encoder.WriteStatusCode(nameof(MonitoredItemMessageContentMask.StatusCode), Value.StatusCode);
            }
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.Status) != 0) {
                encoder.WriteString(nameof(MonitoredItemMessageContentMask.Status), StatusCode.LookupSymbolicId(Value.StatusCode.Code));
            }
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.EndpointUrl) != 0) {
                encoder.WriteString(nameof(MonitoredItemMessageContentMask.EndpointUrl), EndpointUrl);
            }
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.SubscriptionId) != 0) {
                encoder.WriteString(nameof(MonitoredItemMessageContentMask.SubscriptionId), SubscriptionId);
            }
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.ApplicationUri) != 0) {
                encoder.WriteString(nameof(MonitoredItemMessageContentMask.ApplicationUri), ApplicationUri);
            }
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.DisplayName) != 0) {
                encoder.WriteString(nameof(MonitoredItemMessageContentMask.DisplayName), DisplayName);
            }
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.Timestamp) != 0) {
                encoder.WriteDateTime(nameof(MonitoredItemMessageContentMask.Timestamp), DateTime.UtcNow);
            }
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.PicoSeconds) != 0) {
                encoder.WriteUInt16(nameof(MonitoredItemMessageContentMask.PicoSeconds), 0);
            }

            encoder.WriteVariant("Value", Value.WrappedValue);

            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.ExtraFields) != 0) {
                if (ExtensionFields != null) {
                    foreach (var field in ExtensionFields) {
                        encoder.WriteString(field.Key, field.Value);
                    }
                }
            }
        }

        /// <inheritdoc/>
        public bool IsEqual(IEncodeable encodeable) {
            if (ReferenceEquals(this, encodeable)) {
                return true;
            }

            return false;
        }
    }
}