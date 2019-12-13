// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Core.Models {
    /// <summary>
    /// Data change filter model extensions
    /// </summary>
    public static class DataChangeFilterModelEx {

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static DataChangeFilterModel Clone(this DataChangeFilterModel model) {
            if (model == null) {
                return null;
            }
            return new DataChangeFilterModel {
                DataChangeTrigger = model.DataChangeTrigger,
                DeadBandType = model.DeadBandType,
                DeadBandValue = model.DeadBandValue
            };
        }
    }
}