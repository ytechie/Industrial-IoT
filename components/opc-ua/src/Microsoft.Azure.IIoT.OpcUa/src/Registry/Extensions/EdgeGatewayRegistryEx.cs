// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry {
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Edge Gateway registry extensions
    /// </summary>
    public static class EdgeGatewayRegistryEx {

        /// <summary>
        /// Find edge gateway.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="publisherId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<EdgeGatewayModel> FindEdgeGatewayAsync(
            this IEdgeGatewayRegistry service, string publisherId,
            CancellationToken ct = default) {
            try {
                return await service.GetEdgeGatewayAsync(publisherId, false, ct);
            }
            catch (ResourceNotFoundException) {
                return null;
            }
        }

        /// <summary>
        /// List all edge gateways
        /// </summary>
        /// <param name="service"></param>
        /// <param name="onlyServerState"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<List<EdgeGatewayModel>> ListAllEdgeGatewaysAsync(
            this IEdgeGatewayRegistry service, bool onlyServerState = false,
            CancellationToken ct = default) {
            var publishers = new List<EdgeGatewayModel>();
            var result = await service.ListEdgeGatewaysAsync(null, onlyServerState, null, ct);
            publishers.AddRange(result.Items);
            while (result.ContinuationToken != null) {
                result = await service.ListEdgeGatewaysAsync(result.ContinuationToken,
                    onlyServerState, null, ct);
                publishers.AddRange(result.Items);
            }
            return publishers;
        }

        /// <summary>
        /// Returns all edge gateway ids from the registry
        /// </summary>
        /// <param name="service"></param>
        /// <param name="onlyServerState"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<List<string>> ListAllEdgeGatewayIdsAsync(
            this IEdgeGatewayRegistry service, bool onlyServerState = false,
            CancellationToken ct = default) {
            var publishers = new List<string>();
            var result = await service.ListEdgeGatewaysAsync(null, onlyServerState, null, ct);
            publishers.AddRange(result.Items.Select(s => s.Id));
            while (result.ContinuationToken != null) {
                result = await service.ListEdgeGatewaysAsync(result.ContinuationToken,
                    onlyServerState, null, ct);
                publishers.AddRange(result.Items.Select(s => s.Id));
            }
            return publishers;
        }
    }
}
