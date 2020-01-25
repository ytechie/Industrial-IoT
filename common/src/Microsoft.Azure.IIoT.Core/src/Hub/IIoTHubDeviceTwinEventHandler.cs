// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub {
    using Microsoft.Azure.IIoT.Hub.Models;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Device twin event handler
    /// </summary>
    public interface IIoTHubDeviceTwinEventHandler {

        /// <summary>
        /// Handles twin
        /// </summary>
        /// <param name="twin"></param>
        /// <param name="opTime"></param>
        /// <param name="ev"></param>
        /// <returns></returns>
        Task<bool> HandleAsync(DeviceTwinModel twin,
            DateTime opTime, DeviceTwinEvent ev);
    }
}
