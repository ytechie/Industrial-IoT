// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Runtime {
    using Microsoft.Azure.IIoT.Module.Framework;
    using Microsoft.Azure.IIoT.Module.Framework.Client;
    using Microsoft.Azure.IIoT.Hub.Module.Client.Runtime;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Runtime;
    using Microsoft.Extensions.Configuration;
    using System;

    /// <summary>
    /// Wraps a configuration root
    /// </summary>
    public class Config : DiagnosticsConfig, IModuleConfig, IClientServicesConfig {

        /// <inheritdoc/>
        public string EdgeHubConnectionString => _module.EdgeHubConnectionString;
        /// <inheritdoc/>
        public bool BypassCertVerification => _module.BypassCertVerification;
        /// <inheritdoc/>
        public TransportOption Transport => _module.Transport;

        /// <inheritdoc/>
        public string AppCertStoreType => _opc.AppCertStoreType;
        /// <inheritdoc/>
        public string PkiRootPath => _opc.PkiRootPath;
        /// <inheritdoc/>
        public string OwnCertPath => _opc.OwnCertPath;
        /// <inheritdoc/>
        public string TrustedCertPath => _opc.TrustedCertPath;
        /// <inheritdoc/>
        public string IssuerCertPath => _opc.IssuerCertPath;
        /// <inheritdoc/>
        public string RejectedCertPath => _opc.RejectedCertPath;
        /// <inheritdoc/>
        public bool AutoAcceptUntrustedCertificates => _opc.AutoAcceptUntrustedCertificates;
        /// <inheritdoc/>
        public string OwnCertX509StorePathDefault => _opc.OwnCertX509StorePathDefault;
        /// <inheritdoc/>
        public TimeSpan? DefaultSessionTimeout => _opc.DefaultSessionTimeout;
        /// <inheritdoc/>
        public TimeSpan? OperationTimeout => _opc.OperationTimeout;

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public Config(IConfiguration configuration) :
            base(configuration) {
            _opc = new ClientServicesConfig(configuration);
            _module = new ModuleConfig(configuration);
        }

        private readonly ClientServicesConfig _opc;
        private readonly ModuleConfig _module;
    }
}
