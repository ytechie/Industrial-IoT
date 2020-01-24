// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Handlers {
    using Microsoft.Azure.IIoT.Hub;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Registry Twin change events
    /// </summary>
    public sealed class DeviceLifecycleEventHandler : IDeviceTelemetryHandler {

        /// <inheritdoc/>
        public string MessageSchema => Hub.MessageSchemaTypes.DeviceLifecycleNotification;

        /// <summary>
        /// Create handler
        /// </summary>
        /// <param name="logger"></param>
        public DeviceLifecycleEventHandler(ILogger logger) {
            _logger = logger ??
                throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task HandleAsync(string deviceId, string moduleId,
            byte[] payload, IDictionary<string, string> properties,
            Func<Task> checkpoint) {

            if (!properties.TryGetValue("opType", out var opType) ||
                !properties.TryGetValue("operationTimestamp", out var ts)) {
                return;
            }

            var patch = Encoding.UTF8.GetString(payload);
            _logger.Information("{deviceId} - {operation} - {ts} - {payload}",
                deviceId, opType, ts, patch);

            // TODO
            await Task.Delay(1);
        }

        /// <inheritdoc/>
        public Task OnBatchCompleteAsync() {
            return Task.CompletedTask;
        }

        private readonly ILogger _logger;
    }
}
