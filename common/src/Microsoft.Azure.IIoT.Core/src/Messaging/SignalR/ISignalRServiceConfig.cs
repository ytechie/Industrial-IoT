﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Messaging.SignalR {

    /// <summary>
    /// SignalR service configuration
    /// </summary>
    public interface ISignalRServiceConfig {

        /// <summary>
        /// SignalR Hub name
        /// </summary>
        string SignalRHubName { get; }

        /// <summary>
        /// SignalR connection string
        /// </summary>
        string SignalRConnString { get; }
    }
}