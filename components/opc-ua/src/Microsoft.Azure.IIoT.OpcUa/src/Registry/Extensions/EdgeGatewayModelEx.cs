// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Service model extensionsedge gateway
    /// </summary>
    public static class EdgeGatewayModelEx {

        /// <summary>
        /// Equality comparison
        /// </summary>
        /// <param name="model"></param>
        /// <param name="that"></param>
        /// <returns></returns>
        public static bool IsSameAs(this IEnumerable<EdgeGatewayModel> model,
            IEnumerable<EdgeGatewayModel> that) {
            if (model == that) {
                return true;
            }
            if (model == null || that == null) {
                return false;
            }
            if (model.Count() != that.Count()) {
                return false;
            }
            return model.All(a => that.Any(b => b.IsSameAs(a)));
        }

        /// <summary>
        /// Equality comparison
        /// </summary>
        /// <param name="model"></param>
        /// <param name="that"></param>
        /// <returns></returns>
        public static bool IsSameAs(this EdgeGatewayModel model,
            EdgeGatewayModel that) {
            if (model == that) {
                return true;
            }
            if (model == null || that == null) {
                return false;
            }
            return that.Id == model.Id;
        }

        /// <summary>
        /// Deep clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static EdgeGatewayModel Clone(this EdgeGatewayModel model) {
            if (model == null) {
                return null;
            }
            return new EdgeGatewayModel {
                Connected = model.Connected,
                Id = model.Id,
                OutOfSync = model.OutOfSync,
                SiteId = model.SiteId
            };
        }
    }
}
