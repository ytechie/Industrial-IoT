﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using System;

    public static partial class ApplicationEventEx {
        /// <summary>
        /// Gateway event extensions
        /// </summary>
        public static class GatewayEventEx {

            /// <summary>
            /// Convert to api model
            /// </summary>
            /// <param name="model"></param>
            /// <returns></returns>
            public static GatewayEventApiModel ToApiModel(
                this GatewayEventModel model) {
                return new GatewayEventApiModel {
                    EventType = (GatewayEventType)model.EventType,
                    IsPatch = model.IsPatch,
                    Gateway = model.Gateway.Map<GatewayApiModel>()
                };
            }
        }
    }
}