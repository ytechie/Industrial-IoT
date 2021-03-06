// <auto-generated>
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.
//
// Code generated by Microsoft (R) AutoRest Code Generator 1.0.0.0
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>

namespace Microsoft.Azure.IIoT.Opc.Registry.Models
{
    using Newtonsoft.Json;
    using System.Linq;

    /// <summary>
    /// Endpoint query
    /// </summary>
    public partial class EndpointRegistrationQueryApiModel
    {
        /// <summary>
        /// Initializes a new instance of the EndpointRegistrationQueryApiModel
        /// class.
        /// </summary>
        public EndpointRegistrationQueryApiModel()
        {
            CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the EndpointRegistrationQueryApiModel
        /// class.
        /// </summary>
        /// <param name="url">Endoint url for direct server access</param>
        /// <param name="certificate">Certificate of the endpoint</param>
        /// <param name="securityMode">Possible values include: 'Best', 'Sign',
        /// 'SignAndEncrypt', 'None'</param>
        /// <param name="securityPolicy">Security policy uri</param>
        /// <param name="activated">Whether the endpoint was activated</param>
        /// <param name="connected">Whether the endpoint is connected on
        /// supervisor.</param>
        /// <param name="endpointState">Possible values include: 'Connecting',
        /// 'NotReachable', 'Busy', 'NoTrust', 'CertificateInvalid', 'Ready',
        /// 'Error'</param>
        /// <param name="includeNotSeenSince">Whether to include endpoints that
        /// were soft deleted</param>
        /// <param name="discovererId">Discoverer id to filter with</param>
        /// <param name="applicationId">Application id to filter</param>
        /// <param name="supervisorId">Supervisor id to filter with</param>
        /// <param name="siteOrGatewayId">Site or gateway id to filter
        /// with</param>
        public EndpointRegistrationQueryApiModel(string url = default(string), byte[] certificate = default(byte[]), SecurityMode? securityMode = default(SecurityMode?), string securityPolicy = default(string), bool? activated = default(bool?), bool? connected = default(bool?), EndpointConnectivityState? endpointState = default(EndpointConnectivityState?), bool? includeNotSeenSince = default(bool?), string discovererId = default(string), string applicationId = default(string), string supervisorId = default(string), string siteOrGatewayId = default(string))
        {
            Url = url;
            Certificate = certificate;
            SecurityMode = securityMode;
            SecurityPolicy = securityPolicy;
            Activated = activated;
            Connected = connected;
            EndpointState = endpointState;
            IncludeNotSeenSince = includeNotSeenSince;
            DiscovererId = discovererId;
            ApplicationId = applicationId;
            SupervisorId = supervisorId;
            SiteOrGatewayId = siteOrGatewayId;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// Gets or sets endoint url for direct server access
        /// </summary>
        [JsonProperty(PropertyName = "url")]
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets certificate of the endpoint
        /// </summary>
        [JsonProperty(PropertyName = "certificate")]
        public byte[] Certificate { get; set; }

        /// <summary>
        /// Gets or sets possible values include: 'Best', 'Sign',
        /// 'SignAndEncrypt', 'None'
        /// </summary>
        [JsonProperty(PropertyName = "securityMode")]
        public SecurityMode? SecurityMode { get; set; }

        /// <summary>
        /// Gets or sets security policy uri
        /// </summary>
        [JsonProperty(PropertyName = "securityPolicy")]
        public string SecurityPolicy { get; set; }

        /// <summary>
        /// Gets or sets whether the endpoint was activated
        /// </summary>
        [JsonProperty(PropertyName = "activated")]
        public bool? Activated { get; set; }

        /// <summary>
        /// Gets or sets whether the endpoint is connected on supervisor.
        /// </summary>
        [JsonProperty(PropertyName = "connected")]
        public bool? Connected { get; set; }

        /// <summary>
        /// Gets or sets possible values include: 'Connecting', 'NotReachable',
        /// 'Busy', 'NoTrust', 'CertificateInvalid', 'Ready', 'Error'
        /// </summary>
        [JsonProperty(PropertyName = "endpointState")]
        public EndpointConnectivityState? EndpointState { get; set; }

        /// <summary>
        /// Gets or sets whether to include endpoints that were soft deleted
        /// </summary>
        [JsonProperty(PropertyName = "includeNotSeenSince")]
        public bool? IncludeNotSeenSince { get; set; }

        /// <summary>
        /// Gets or sets discoverer id to filter with
        /// </summary>
        [JsonProperty(PropertyName = "discovererId")]
        public string DiscovererId { get; set; }

        /// <summary>
        /// Gets or sets application id to filter
        /// </summary>
        [JsonProperty(PropertyName = "applicationId")]
        public string ApplicationId { get; set; }

        /// <summary>
        /// Gets or sets supervisor id to filter with
        /// </summary>
        [JsonProperty(PropertyName = "supervisorId")]
        public string SupervisorId { get; set; }

        /// <summary>
        /// Gets or sets site or gateway id to filter with
        /// </summary>
        [JsonProperty(PropertyName = "siteOrGatewayId")]
        public string SiteOrGatewayId { get; set; }

    }
}
