// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using System;

    public static partial class ApplicationEventEx {
        /// <summary>
        /// Publisher event extensions
        /// </summary>
        public static class PublisherEventEx {

            /// <summary>
            /// Convert to api model
            /// </summary>
            /// <param name="model"></param>
            /// <returns></returns>
            public static PublisherEventApiModel ToApiModel(
                this PublisherEventModel model) {
                return new PublisherEventApiModel {
                    EventType = (PublisherEventType)model.EventType,
                    IsPatch = model.IsPatch,
                    Publisher = model.Supervisor.Map<PublisherApiModel>()
                };
            }
        }
    }
}