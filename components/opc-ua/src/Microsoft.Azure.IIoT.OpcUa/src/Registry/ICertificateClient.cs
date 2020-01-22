// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Certificate client
    /// </summary>
    public interface ICertificateClient {

        /// <summary>
        /// Get endpoint certificate
        /// </summary>
        /// <param name="registration"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<byte[]> GetEndpointCertificateAsync(
            EndpointRegistrationModel registration, CancellationToken ct);
    }
}
