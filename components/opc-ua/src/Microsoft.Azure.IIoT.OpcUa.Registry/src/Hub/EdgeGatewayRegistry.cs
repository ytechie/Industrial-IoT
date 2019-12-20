// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Services {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Hub;
    using Serilog;
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Threading;

    /// <summary>
    /// Edge registry which uses the IoT Hub twin services for supervisor
    /// identity management.
    /// </summary>
    public sealed class EdgeGatewayRegistry : IEdgeGatewayRegistry {

        /// <summary>
        /// Create registry services
        /// </summary>
        /// <param name="iothub"></param>
        /// <param name="logger"></param>
        public EdgeGatewayRegistry(IIoTHubTwinServices iothub, ILogger logger) {
            _iothub = iothub ?? throw new ArgumentNullException(nameof(iothub));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<EdgeGatewayModel> GetEdgeAsync(string gatewayId,
            bool onlyServerState, CancellationToken ct) {
            if (string.IsNullOrEmpty(gatewayId)) {
                throw new ArgumentException(nameof(gatewayId));
            }
            var deviceId = gatewayId;
            var device = await _iothub.GetAsync(deviceId, null, ct);
            var registration = device.ToRegistration(onlyServerState)
                as EdgeRegistration;
            if (registration == null) {
                throw new ResourceNotFoundException(
                    $"{gatewayId} is not a supervisor registration.");
            }
            return registration.ToServiceModel();
        }

        /// <inheritdoc/>
        public async Task UpdateEdgeAsync(string gatewayId,
            EdgeGatewayUpdateModel request, CancellationToken ct) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(gatewayId)) {
                throw new ArgumentException(nameof(gatewayId));
            }

            // Get existing endpoint and compare to see if we need to patch.
            var deviceId = gatewayId;

            while (true) {
                try {
                    var twin = await _iothub.GetAsync(deviceId, null, ct);
                    if (twin.Id != deviceId) {
                        throw new ArgumentException("Id must be same as twin to patch",
                            nameof(gatewayId));
                    }

                    var registration = twin.ToRegistration(true) as EdgeRegistration;
                    if (registration == null) {
                        throw new ResourceNotFoundException(
                            $"{gatewayId} is not a supervisor registration.");
                    }

                    // Update registration from update request
                    var patched = registration.ToServiceModel();

                    if (request.SiteId != null) {
                        patched.SiteId = string.IsNullOrEmpty(request.SiteId) ?
                            null : request.SiteId;
                    }
                    // Patch
                    await _iothub.PatchAsync(registration.Patch(
                        patched.ToEdgeRegistration()), false, ct);
                    return;
                }
                catch (ResourceOutOfDateException ex) {
                    _logger.Debug(ex, "Retrying updating supervisor...");
                    continue;
                }
            }
        }

        /// <inheritdoc/>
        public async Task<EdgeGatewayListModel> ListEdgesAsync(
            string continuation, bool onlyServerState, int? pageSize, CancellationToken ct) {
            var query = "SELECT * FROM devices WHERE " +
                $"properties.reported.{TwinProperty.Type} = 'supervisor' " +
                $"AND NOT IS_DEFINED(tags.{nameof(BaseRegistration.NotSeenSince)})";
            var devices = await _iothub.QueryDeviceTwinsAsync(query, continuation, pageSize, ct);
            return new EdgeGatewayListModel {
                ContinuationToken = devices.ContinuationToken,
                Items = devices.Items
                    .Select(t => t.ToEdgeRegistration(onlyServerState))
                    .Select(s => s.ToServiceModel())
                    .ToList()
            };
        }

        /// <inheritdoc/>
        public async Task<EdgeGatewayListModel> QueryEdgesAsync(
            EdgeGatewayQueryModel model, bool onlyServerState, int? pageSize, CancellationToken ct) {

            var query = "SELECT * FROM devices.modules WHERE " +
                $"properties.reported.{TwinProperty.Type} = 'supervisor'";

            if (model?.SiteId != null) {
                // If site id provided, include it in search
                query += $"AND (properties.reported.{TwinProperty.SiteId} = " +
                    $"'{model.SiteId}' OR properties.desired.{TwinProperty.SiteId} = " +
                    $"'{model.SiteId}')";
            }
            if (model?.Connected != null) {
                // If flag provided, include it in search
                if (model.Connected.Value) {
                    query += $"AND connectionState = 'Connected' ";
                    // Do not use connected property as module might have exited before updating.
                }
                else {
                    query += $"AND (connectionState = 'Disconnected' " +
                        $"OR properties.reported.{TwinProperty.Connected} != true) ";
                }
            }

            var queryResult = await _iothub.QueryDeviceTwinsAsync(query, null, pageSize, ct);
            return new EdgeGatewayListModel {
                ContinuationToken = queryResult.ContinuationToken,
                Items = queryResult.Items
                    .Select(t => t.ToEdgeRegistration(onlyServerState))
                    .Select(s => s.ToServiceModel())
                    .ToList()
            };
        }

        private readonly IIoTHubTwinServices _iothub;
        private readonly ILogger _logger;
    }
}
