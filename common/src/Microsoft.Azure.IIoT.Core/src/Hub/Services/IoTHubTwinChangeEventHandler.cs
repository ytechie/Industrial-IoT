﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Services {
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Utils;
    using Newtonsoft.Json;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    using System.Linq;

    /// <summary>
    /// Registry Twin change events
    /// </summary>
    public sealed class IoTHubTwinChangeEventHandler : IDeviceTelemetryHandler {

        /// <inheritdoc/>
        public string MessageSchema => Hub.MessageSchemaTypes.TwinChangeNotification;

        /// <summary>
        /// Create handler
        /// </summary>
        /// <param name="handlers"></param>
        /// <param name="logger"></param>
        public IoTHubTwinChangeEventHandler(IEnumerable<IIoTHubDeviceTwinEventHandler> handlers, ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _handlers = handlers.ToList();
        }

        /// <inheritdoc/>
        public async Task HandleAsync(string deviceId, string moduleId,
            byte[] payload, IDictionary<string, string> properties,
            Func<Task> checkpoint) {

            if (!properties.TryGetValue("opType", out var opType) ||
                !properties.TryGetValue("operationTimestamp", out var ts)) {
                return;
            }

            var twin = Try.Op(() => JsonConvertEx.DeserializeObject<DeviceTwinModel>(
                Encoding.UTF8.GetString(payload)));
            if (twin == null) {
                return;
            }

            DeviceTwinEvent operation;
            switch (opType) {
                case "replaceTwin":
                    operation = DeviceTwinEvent.Create;
                    break;
                case "updateTwin":
                    operation = DeviceTwinEvent.Update;
                    break;
                default:
                    // Unknown
                    return;
            }

            twin.ModuleId = moduleId;
            twin.Id = deviceId;
            DateTime.TryParse(ts, out var time);
            foreach (var handler in _handlers) {
                var handled = await handler.HandleAsync(twin, time, operation);
                if (handled) {
                    return; // Done
                }
            }
        }

        /// <inheritdoc/>
        public Task OnBatchCompleteAsync() {
            return Task.CompletedTask;
        }

        private readonly ILogger _logger;
        private readonly List<IIoTHubDeviceTwinEventHandler> _handlers;
    }
}
