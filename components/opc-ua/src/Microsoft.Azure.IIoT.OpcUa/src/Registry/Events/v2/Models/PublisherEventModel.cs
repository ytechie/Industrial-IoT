// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Events.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;

    /// <summary>
    /// Publisher event
    /// </summary>
    public class PublisherEventApiModel {

        /// <summary>
        /// Event type
        /// </summary>
        public PublisherEventType EventType { get; set; }

        /// <summary>
        /// Publisher
        /// </summary>
        public PublisherModel Publisher { get; set; }

        /// <summary>
        /// The information is provided as a patch
        /// </summary>
        public bool? IsPatch { get; set; }
    }
}