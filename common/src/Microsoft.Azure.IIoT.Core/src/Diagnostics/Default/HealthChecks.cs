// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Diagnostics {
    using Microsoft.Extensions.Diagnostics.HealthChecks;
    using Microsoft.Extensions.Options;
    using System.Collections.Generic;

    /// <summary>
    /// Health checks
    /// </summary>
    public sealed class HealthChecks : IOptions<HealthCheckServiceOptions> {

        /// <inheritdoc/>
        public HealthCheckServiceOptions Value { get; }

        /// <summary>
        /// Register checks
        /// </summary>
        /// <param name="checks"></param>
        public HealthChecks(IEnumerable<IHealthCheck> checks) {
            Value = new HealthCheckServiceOptions();
            foreach (var check in checks) {
                Value.Registrations.Add(new HealthCheckRegistration())
            }
        }
    }
}