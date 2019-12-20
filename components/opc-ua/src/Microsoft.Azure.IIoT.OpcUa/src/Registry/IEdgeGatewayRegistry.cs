// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Edge Gateway registry
    /// </summary>
    public interface IEdgeGatewayRegistry {

        /// <summary>
        /// Get all gateways in paged form
        /// </summary>
        /// <param name="continuation"></param>
        /// <param name="onlyServerState"></param>
        /// <param name="pageSize"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<EdgeGatewayListModel> ListEdgeGatewaysAsync(
            string continuation, bool onlyServerState = false,
            int? pageSize = null, CancellationToken ct = default);

        /// <summary>
        /// Find gateways using specific criterias.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="onlyServerState"></param>
        /// <param name="pageSize"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<EdgeGatewayListModel> QueryEdgeGatewaysAsync(
            EdgeGatewayQueryModel query, bool onlyServerState = false,
            int? pageSize = null,
            CancellationToken ct = default);

        /// <summary>
        /// Get gateway registration by identifer.
        /// </summary>
        /// <param name="onlyServerState"></param>
        /// <param name="ct"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<EdgeGatewayModel> GetEdgeGatewayAsync(
            string id, bool onlyServerState = false,
            CancellationToken ct = default);

        /// <summary>
        /// Update gateway
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        Task UpdateEdgeGatewayAsync(string id,
            EdgeGatewayUpdateModel request,
            CancellationToken ct = default);
    }
}
