// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Models {
    using Opc.Ua;

    /// <summary>
    /// Monitored item status
    /// </summary>
    public class MonitoredItemStatusModel {

        /// <summary>
        /// The identifier assigned by the server.
        /// </summary>
        public uint? ServerId { get; set; }

        /// <summary>
        /// Any error condition associated with the monitored item.
        /// </summary>
        public ServiceResult Error { get; set; }
    }
}