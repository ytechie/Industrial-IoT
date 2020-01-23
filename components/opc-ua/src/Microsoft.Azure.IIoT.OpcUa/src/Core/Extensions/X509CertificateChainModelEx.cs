// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Core.Models {
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Certificate Chain extensions
    /// </summary>
    public static class X509CertificateChainModelEx {

        /// <summary>
        /// Convert raw buffer to certificate chain
        /// </summary>
        /// <param name="rawCertificates"></param>
        /// <returns></returns>
        public static X509CertificateChainModel ToCertificateChain(this byte[] rawCertificates) {
            if (rawCertificates == null) {
                return null;
            }
            var certificates = new List<X509CertificateModel>();
            while (true) {
                var cur = new X509Certificate2(rawCertificates);
                certificates.Add(cur.ToServiceModel());
                if (cur.RawData.Length >= rawCertificates.Length) {
                    break;
                }
                rawCertificates = rawCertificates.AsSpan()
                    .Slice(cur.RawData.Length)
                    .ToArray();
            }
            return new X509CertificateChainModel {
                Chain = certificates
            };
        }
    }
}
