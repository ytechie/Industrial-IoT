// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Core.Models {
    /// <summary>
    /// Aggregate filter model extensions
    /// </summary>
    public static class AggregateFilterModelEx {

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static AggregateFilterModel Clone(this AggregateFilterModel model) {
            if (model == null) {
                return null;
            }
            return new AggregateFilterModel {
                AggregateTypeId = model.AggregateTypeId,
                AggregateConfiguration = model.AggregateConfiguration.Clone(),
                ProcessingInterval = model.ProcessingInterval,
                StartTime = model.StartTime
            };
        }
    }

}