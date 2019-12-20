// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Edge gateway registration extensions
    /// </summary>
    public static class EdgeGatewayRegistrationEx {

        /// <summary>
        /// Create device twin
        /// </summary>
        /// <param name="registration"></param>
        /// <returns></returns>
        public static DeviceTwinModel ToDeviceTwin(this EdgeGatewayRegistration registration) {
            return Patch(null, registration);
        }

        /// <summary>
        /// Create patch twin model to upload
        /// </summary>
        /// <param name="existing"></param>
        /// <param name="update"></param>
        public static DeviceTwinModel Patch(this EdgeGatewayRegistration existing,
            EdgeGatewayRegistration update) {

            var twin = new DeviceTwinModel {
                Etag = existing?.Etag,
                Tags = new Dictionary<string, JToken>(),
                Properties = new TwinPropertiesModel {
                    Desired = new Dictionary<string, JToken>()
                }
            };

            // Tags

            if (update?.IsDisabled != null && update.IsDisabled != existing?.IsDisabled) {
                twin.Tags.Add(nameof(EdgeGatewayRegistration.IsDisabled), (update?.IsDisabled ?? false) ?
                    true : (bool?)null);
                twin.Tags.Add(nameof(EdgeGatewayRegistration.NotSeenSince), (update?.IsDisabled ?? false) ?
                    DateTime.UtcNow : (DateTime?)null);
            }

            if (update?.SiteOrSupervisorId != existing?.SiteOrSupervisorId) {
                twin.Tags.Add(nameof(EdgeGatewayRegistration.SiteOrSupervisorId),
                    update?.SiteOrSupervisorId);
            }

            // Settings

            if (update?.SiteId != existing?.SiteId) {
                twin.Properties.Desired.Add(TwinProperty.SiteId, update?.SiteId);
            }

            twin.Tags.Add(nameof(EdgeGatewayRegistration.DeviceType), update?.DeviceType);
            twin.Id = update?.DeviceId ?? existing?.DeviceId;
            return twin;
        }

        /// <summary>
        /// Decode tags and property into registration object
        /// </summary>
        /// <param name="twin"></param>
        /// <param name="properties"></param>
        /// <returns></returns>
        public static EdgeGatewayRegistration ToEdgeGatewayRegistration(this DeviceTwinModel twin,
            Dictionary<string, JToken> properties) {
            if (twin == null) {
                return null;
            }

            var tags = twin.Tags ?? new Dictionary<string, JToken>();
            var connected = twin.IsConnected();

            var registration = new EdgeGatewayRegistration {
                // Device

                DeviceId = twin.Id,
                ModuleId = twin.ModuleId,
                Etag = twin.Etag,

                // Tags

                IsDisabled =
                    tags.GetValueOrDefault<bool>(nameof(EdgeGatewayRegistration.IsDisabled), null),
                NotSeenSince =
                    tags.GetValueOrDefault<DateTime>(nameof(EdgeGatewayRegistration.NotSeenSince), null),

                // Properties

                SiteId =
                    properties.GetValueOrDefault<string>(TwinProperty.SiteId, null),
                Connected = connected ??
                    properties.GetValueOrDefault(TwinProperty.Connected, false),
                Type =
                    properties.GetValueOrDefault<string>(TwinProperty.Type, null)
            };
            return registration;
        }

        /// <summary>
        /// Get supervisor registration from twin
        /// </summary>
        /// <param name="twin"></param>
        /// <param name="onlyServerState"></param>
        /// <returns></returns>
        public static EdgeGatewayRegistration ToEdgeGatewayRegistration(this DeviceTwinModel twin,
            bool onlyServerState) {
            return ToEdgeGatewayRegistration(twin, onlyServerState, out var tmp);
        }

        /// <summary>
        /// Make sure to get the registration information from the right place.
        /// Reported (truth) properties take precedence over desired. However,
        /// if there is nothing reported, it means the endpoint is not currently
        /// serviced, thus we use desired as if they are attributes of the
        /// endpoint.
        /// </summary>
        /// <param name="twin"></param>
        /// <param name="connected"></param>
        /// <param name="onlyServerState">Only desired endpoint should be returned
        /// this means that you will look at stale information.</param>
        /// <returns></returns>
        public static EdgeGatewayRegistration ToEdgeGatewayRegistration(this DeviceTwinModel twin,
            bool onlyServerState, out bool connected) {

            if (twin == null) {
                connected = false;
                return null;
            }
            if (twin.Tags == null) {
                twin.Tags = new Dictionary<string, JToken>();
            }

            var consolidated =
                ToEdgeGatewayRegistration(twin, twin.GetConsolidatedProperties());
            var desired = (twin.Properties?.Desired == null) ? null :
                ToEdgeGatewayRegistration(twin, twin.Properties.Desired);

            connected = consolidated.Connected;
            if (desired != null) {
                desired.Connected = connected;
                if (desired.SiteId == null && consolidated.SiteId != null) {
                    // Not set by user, but by config, so fake user desiring it.
                    desired.SiteId = consolidated.SiteId;
                }
            }

            if (!onlyServerState) {
                consolidated._isInSync = consolidated.IsInSyncWith(desired);
                return consolidated;
            }
            if (desired != null) {
                desired._isInSync = desired.IsInSyncWith(consolidated);
            }
            return desired;
        }

        /// <summary>
        /// Patch this registration and create patch twin model to upload
        /// </summary>
        /// <param name="model"></param>
        /// <param name="disabled"></param>
        public static EdgeGatewayRegistration ToEdgeGatewayRegistration(
            this EdgeGatewayModel model, bool? disabled = null) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            var deviceId = model.Id;
            return new EdgeGatewayRegistration {
                IsDisabled = disabled,
                SupervisorId = model.Id,
                DeviceId = deviceId,
                Connected = model.Connected ?? false,
                SiteId = model.SiteId,
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="registration"></param>
        /// <returns></returns>
        public static EdgeGatewayModel ToServiceModel(this EdgeGatewayRegistration registration) {
            return new EdgeGatewayModel {
                Id = registration.SupervisorId,
                SiteId = registration.SiteId,
                Connected = registration.IsConnected() ? true : (bool?)null,
                OutOfSync = registration.IsConnected() && !registration._isInSync ? true : (bool?)null
            };
        }

        /// <summary>
        /// Flag twin as synchronized - i.e. it matches the other.
        /// </summary>
        /// <param name="registration"></param>
        /// <param name="other"></param>
        internal static bool IsInSyncWith(this EdgeGatewayRegistration registration,
            EdgeGatewayRegistration other) {
            return
                other != null &&
                registration.SiteId == other.SiteId;
        }
    }
}
