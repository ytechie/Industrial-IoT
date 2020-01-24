// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Events.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;

    /// <summary>
    /// Supervisor event
    /// </summary>
    public class SupervisorEventApiModel {

        /// <summary>
        /// Event type
        /// </summary>
        public SupervisorEventType EventType { get; set; }

        /// <summary>
        /// Supervisor
        /// </summary>
        public SupervisorModel Supervisor { get; set; }

        /// <summary>
        /// The information is provided as a patch
        /// </summary>
        public bool? IsPatch { get; set; }
    }
}