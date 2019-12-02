﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {

    /// <summary>
    /// Monitored item
    /// </summary>
    public class MonitoredItemModel {

        /// <summary>
        /// Node id
        /// </summary>
        public string NodeId { get; set; }

        /// <summary>
        /// Sampling interval
        /// </summary>
        public int? SamplingInterval { get; set; }

        /// <summary>
        /// Heartbeat interval
        /// </summary>
        public int? HeartbeatInterval { get; set; }

        /// <summary>
        /// Queue size
        /// </summary>
        public uint? QueueSize { get; set; }

        /// <summary>
        /// Discard new values if queue is full
        /// </summary>
        public bool? DiscardNew { get; set; }

        /// <summary>
        /// Skip first
        /// </summary>
        public bool? SkipFirst { get; set; }

        /// <summary>
        /// Data change filter
        /// </summary>
        public DataChangeFilterType? DataChangeFilter { get; set; }

        /// <summary>
        /// Dead band
        /// </summary>
        public DeadbandType? DeadBandType { get; set; }

        /// <summary>
        /// Dead band value
        /// </summary>
        public double? DeadBandValue { get; set; }
    }
}