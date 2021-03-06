/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for
 * license information.
 *
 * Code generated by Microsoft (R) AutoRest Code Generator 1.0.0.0
 * Changes may cause incorrect behavior and will be lost if the code is
 * regenerated.
 */

package com.microsoft.azure.iiot.opc.vault.models;

import com.fasterxml.jackson.annotation.JsonProperty;

/**
 * Finish request results.
 */
public class FinishSigningRequestResponseApiModel {
    /**
     * The request property.
     */
    @JsonProperty(value = "request")
    private CertificateRequestRecordApiModel request;

    /**
     * The certificate property.
     */
    @JsonProperty(value = "certificate")
    private X509CertificateApiModel certificate;

    /**
     * Get the request value.
     *
     * @return the request value
     */
    public CertificateRequestRecordApiModel request() {
        return this.request;
    }

    /**
     * Set the request value.
     *
     * @param request the request value to set
     * @return the FinishSigningRequestResponseApiModel object itself.
     */
    public FinishSigningRequestResponseApiModel withRequest(CertificateRequestRecordApiModel request) {
        this.request = request;
        return this;
    }

    /**
     * Get the certificate value.
     *
     * @return the certificate value
     */
    public X509CertificateApiModel certificate() {
        return this.certificate;
    }

    /**
     * Set the certificate value.
     *
     * @param certificate the certificate value to set
     * @return the FinishSigningRequestResponseApiModel object itself.
     */
    public FinishSigningRequestResponseApiModel withCertificate(X509CertificateApiModel certificate) {
        this.certificate = certificate;
        return this;
    }

}
