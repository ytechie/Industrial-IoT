// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Core.Models {
    /// <summary>
    /// Aggregate configuration model extensions
    /// </summary>
    public static class AggregateConfigurationModelEx {

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static AggregateConfigurationModel Clone(this AggregateConfigurationModel model) {
            if (model == null) {
                return null;
            }
            return new AggregateConfigurationModel {
                PercentDataBad = model.PercentDataBad,
                PercentDataGood = model.PercentDataGood,
                TreatUncertainAsBad = model.TreatUncertainAsBad,
                UseServerCapabilitiesDefaults = model.UseServerCapabilitiesDefaults,
                UseSlopedExtrapolation = model.UseSlopedExtrapolation
            };
        }
    }

}