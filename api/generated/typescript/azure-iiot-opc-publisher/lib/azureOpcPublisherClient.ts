/*
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for
 * license information.
 *
 * Code generated by Microsoft (R) AutoRest Code Generator 1.0.0.0
 * Changes may cause incorrect behavior and will be lost if the code is
 * regenerated.
 */

import * as msRest from "@azure/ms-rest-js";
import * as Models from "./models";
import * as Mappers from "./models/mappers";
import * as Parameters from "./models/parameters";
import { AzureOpcPublisherClientContext } from "./azureOpcPublisherClientContext";

class AzureOpcPublisherClient extends AzureOpcPublisherClientContext {
  /**
   * Initializes a new instance of the AzureOpcPublisherClient class.
   * @param credentials Subscription credentials which uniquely identify client subscription.
   * @param [options] The parameter options
   */
  constructor(credentials: msRest.ServiceClientCredentials, options?: Models.AzureOpcPublisherClientOptions) {
    super(credentials, options);
  }

  /**
   * Register a client to receive publisher samples through SignalR.
   * @summary Subscribe to receive samples
   * @param endpointId The endpoint to subscribe to
   * @param [options] The optional parameters
   * @returns Promise<msRest.RestResponse>
   */
  subscribe(endpointId: string, options?: Models.AzureOpcPublisherClientSubscribeOptionalParams): Promise<msRest.RestResponse>;
  /**
   * @param endpointId The endpoint to subscribe to
   * @param callback The callback
   */
  subscribe(endpointId: string, callback: msRest.ServiceCallback<void>): void;
  /**
   * @param endpointId The endpoint to subscribe to
   * @param options The optional parameters
   * @param callback The callback
   */
  subscribe(endpointId: string, options: Models.AzureOpcPublisherClientSubscribeOptionalParams, callback: msRest.ServiceCallback<void>): void;
  subscribe(endpointId: string, options?: Models.AzureOpcPublisherClientSubscribeOptionalParams | msRest.ServiceCallback<void>, callback?: msRest.ServiceCallback<void>): Promise<msRest.RestResponse> {
    return this.sendOperationRequest(
      {
        endpointId,
        options
      },
      subscribeOperationSpec,
      callback);
  }

  /**
   * Unregister a client and stop it from receiving samples.
   * @summary Unsubscribe from receiving samples.
   * @param endpointId The endpoint to unsubscribe from
   * @param userId The user id that will not receive any more published samples
   * @param [options] The optional parameters
   * @returns Promise<msRest.RestResponse>
   */
  unsubscribe(endpointId: string, userId: string, options?: msRest.RequestOptionsBase): Promise<msRest.RestResponse>;
  /**
   * @param endpointId The endpoint to unsubscribe from
   * @param userId The user id that will not receive any more published samples
   * @param callback The callback
   */
  unsubscribe(endpointId: string, userId: string, callback: msRest.ServiceCallback<void>): void;
  /**
   * @param endpointId The endpoint to unsubscribe from
   * @param userId The user id that will not receive any more published samples
   * @param options The optional parameters
   * @param callback The callback
   */
  unsubscribe(endpointId: string, userId: string, options: msRest.RequestOptionsBase, callback: msRest.ServiceCallback<void>): void;
  unsubscribe(endpointId: string, userId: string, options?: msRest.RequestOptionsBase | msRest.ServiceCallback<void>, callback?: msRest.ServiceCallback<void>): Promise<msRest.RestResponse> {
    return this.sendOperationRequest(
      {
        endpointId,
        userId,
        options
      },
      unsubscribeOperationSpec,
      callback);
  }

  /**
   * Start publishing variable node values to IoT Hub. The endpoint must be activated and connected
   * and the module client and server must trust each other.
   * @summary Start publishing node values
   * @param endpointId The identifier of the activated endpoint.
   * @param body The publish request
   * @param [options] The optional parameters
   * @returns Promise<Models.StartPublishingValuesResponse>
   */
  startPublishingValues(endpointId: string, body: Models.PublishStartRequestApiModel, options?: msRest.RequestOptionsBase): Promise<Models.StartPublishingValuesResponse>;
  /**
   * @param endpointId The identifier of the activated endpoint.
   * @param body The publish request
   * @param callback The callback
   */
  startPublishingValues(endpointId: string, body: Models.PublishStartRequestApiModel, callback: msRest.ServiceCallback<Models.PublishStartResponseApiModel>): void;
  /**
   * @param endpointId The identifier of the activated endpoint.
   * @param body The publish request
   * @param options The optional parameters
   * @param callback The callback
   */
  startPublishingValues(endpointId: string, body: Models.PublishStartRequestApiModel, options: msRest.RequestOptionsBase, callback: msRest.ServiceCallback<Models.PublishStartResponseApiModel>): void;
  startPublishingValues(endpointId: string, body: Models.PublishStartRequestApiModel, options?: msRest.RequestOptionsBase | msRest.ServiceCallback<Models.PublishStartResponseApiModel>, callback?: msRest.ServiceCallback<Models.PublishStartResponseApiModel>): Promise<Models.StartPublishingValuesResponse> {
    return this.sendOperationRequest(
      {
        endpointId,
        body,
        options
      },
      startPublishingValuesOperationSpec,
      callback) as Promise<Models.StartPublishingValuesResponse>;
  }

  /**
   * Stop publishing variable node values to IoT Hub. The endpoint must be activated and connected
   * and the module client and server must trust each other.
   * @summary Stop publishing node values
   * @param endpointId The identifier of the activated endpoint.
   * @param body The unpublish request
   * @param [options] The optional parameters
   * @returns Promise<Models.StopPublishingValuesResponse>
   */
  stopPublishingValues(endpointId: string, body: Models.PublishStopRequestApiModel, options?: msRest.RequestOptionsBase): Promise<Models.StopPublishingValuesResponse>;
  /**
   * @param endpointId The identifier of the activated endpoint.
   * @param body The unpublish request
   * @param callback The callback
   */
  stopPublishingValues(endpointId: string, body: Models.PublishStopRequestApiModel, callback: msRest.ServiceCallback<Models.PublishStopResponseApiModel>): void;
  /**
   * @param endpointId The identifier of the activated endpoint.
   * @param body The unpublish request
   * @param options The optional parameters
   * @param callback The callback
   */
  stopPublishingValues(endpointId: string, body: Models.PublishStopRequestApiModel, options: msRest.RequestOptionsBase, callback: msRest.ServiceCallback<Models.PublishStopResponseApiModel>): void;
  stopPublishingValues(endpointId: string, body: Models.PublishStopRequestApiModel, options?: msRest.RequestOptionsBase | msRest.ServiceCallback<Models.PublishStopResponseApiModel>, callback?: msRest.ServiceCallback<Models.PublishStopResponseApiModel>): Promise<Models.StopPublishingValuesResponse> {
    return this.sendOperationRequest(
      {
        endpointId,
        body,
        options
      },
      stopPublishingValuesOperationSpec,
      callback) as Promise<Models.StopPublishingValuesResponse>;
  }

  /**
   * Returns currently published node ids for an endpoint. The endpoint must be activated and
   * connected and the module client and server must trust each other.
   * @summary Get currently published nodes
   * @param endpointId The identifier of the activated endpoint.
   * @param body The list request
   * @param [options] The optional parameters
   * @returns Promise<Models.GetFirstListOfPublishedNodesResponse>
   */
  getFirstListOfPublishedNodes(endpointId: string, body: Models.PublishedItemListRequestApiModel, options?: msRest.RequestOptionsBase): Promise<Models.GetFirstListOfPublishedNodesResponse>;
  /**
   * @param endpointId The identifier of the activated endpoint.
   * @param body The list request
   * @param callback The callback
   */
  getFirstListOfPublishedNodes(endpointId: string, body: Models.PublishedItemListRequestApiModel, callback: msRest.ServiceCallback<Models.PublishedItemListResponseApiModel>): void;
  /**
   * @param endpointId The identifier of the activated endpoint.
   * @param body The list request
   * @param options The optional parameters
   * @param callback The callback
   */
  getFirstListOfPublishedNodes(endpointId: string, body: Models.PublishedItemListRequestApiModel, options: msRest.RequestOptionsBase, callback: msRest.ServiceCallback<Models.PublishedItemListResponseApiModel>): void;
  getFirstListOfPublishedNodes(endpointId: string, body: Models.PublishedItemListRequestApiModel, options?: msRest.RequestOptionsBase | msRest.ServiceCallback<Models.PublishedItemListResponseApiModel>, callback?: msRest.ServiceCallback<Models.PublishedItemListResponseApiModel>): Promise<Models.GetFirstListOfPublishedNodesResponse> {
    return this.sendOperationRequest(
      {
        endpointId,
        body,
        options
      },
      getFirstListOfPublishedNodesOperationSpec,
      callback) as Promise<Models.GetFirstListOfPublishedNodesResponse>;
  }

  /**
   * Returns next set of currently published node ids for an endpoint. The endpoint must be activated
   * and connected and the module client and server must trust each other.
   * @summary Get next set of published nodes
   * @param endpointId The identifier of the activated endpoint.
   * @param continuationToken The continuation token to continue with
   * @param [options] The optional parameters
   * @returns Promise<Models.GetNextListOfPublishedNodesResponse>
   */
  getNextListOfPublishedNodes(endpointId: string, continuationToken: string, options?: msRest.RequestOptionsBase): Promise<Models.GetNextListOfPublishedNodesResponse>;
  /**
   * @param endpointId The identifier of the activated endpoint.
   * @param continuationToken The continuation token to continue with
   * @param callback The callback
   */
  getNextListOfPublishedNodes(endpointId: string, continuationToken: string, callback: msRest.ServiceCallback<Models.PublishedItemListResponseApiModel>): void;
  /**
   * @param endpointId The identifier of the activated endpoint.
   * @param continuationToken The continuation token to continue with
   * @param options The optional parameters
   * @param callback The callback
   */
  getNextListOfPublishedNodes(endpointId: string, continuationToken: string, options: msRest.RequestOptionsBase, callback: msRest.ServiceCallback<Models.PublishedItemListResponseApiModel>): void;
  getNextListOfPublishedNodes(endpointId: string, continuationToken: string, options?: msRest.RequestOptionsBase | msRest.ServiceCallback<Models.PublishedItemListResponseApiModel>, callback?: msRest.ServiceCallback<Models.PublishedItemListResponseApiModel>): Promise<Models.GetNextListOfPublishedNodesResponse> {
    return this.sendOperationRequest(
      {
        endpointId,
        continuationToken,
        options
      },
      getNextListOfPublishedNodesOperationSpec,
      callback) as Promise<Models.GetNextListOfPublishedNodesResponse>;
  }
}

// Operation Specifications
const serializer = new msRest.Serializer(Mappers);
const subscribeOperationSpec: msRest.OperationSpec = {
  httpMethod: "PUT",
  path: "v2/monitor/{endpointId}/samples",
  urlParameters: [
    Parameters.endpointId
  ],
  requestBody: {
    parameterPath: [
      "options",
      "body"
    ],
    mapper: {
      serializedName: "body",
      type: {
        name: "String"
      }
    }
  },
  responses: {
    200: {},
    default: {}
  },
  serializer
};

const unsubscribeOperationSpec: msRest.OperationSpec = {
  httpMethod: "DELETE",
  path: "v2/monitor/{endpointId}/samples/{userId}",
  urlParameters: [
    Parameters.endpointId,
    Parameters.userId
  ],
  contentType: "application/json; charset=utf-8",
  responses: {
    200: {},
    default: {}
  },
  serializer
};

const startPublishingValuesOperationSpec: msRest.OperationSpec = {
  httpMethod: "POST",
  path: "v2/publish/{endpointId}/start",
  urlParameters: [
    Parameters.endpointId
  ],
  requestBody: {
    parameterPath: "body",
    mapper: {
      ...Mappers.PublishStartRequestApiModel,
      required: true
    }
  },
  responses: {
    200: {
      bodyMapper: Mappers.PublishStartResponseApiModel
    },
    default: {}
  },
  serializer
};

const stopPublishingValuesOperationSpec: msRest.OperationSpec = {
  httpMethod: "POST",
  path: "v2/publish/{endpointId}/stop",
  urlParameters: [
    Parameters.endpointId
  ],
  requestBody: {
    parameterPath: "body",
    mapper: {
      ...Mappers.PublishStopRequestApiModel,
      required: true
    }
  },
  responses: {
    200: {
      bodyMapper: Mappers.PublishStopResponseApiModel
    },
    default: {}
  },
  serializer
};

const getFirstListOfPublishedNodesOperationSpec: msRest.OperationSpec = {
  httpMethod: "POST",
  path: "v2/publish/{endpointId}",
  urlParameters: [
    Parameters.endpointId
  ],
  requestBody: {
    parameterPath: "body",
    mapper: {
      ...Mappers.PublishedItemListRequestApiModel,
      required: true
    }
  },
  responses: {
    200: {
      bodyMapper: Mappers.PublishedItemListResponseApiModel
    },
    default: {}
  },
  serializer
};

const getNextListOfPublishedNodesOperationSpec: msRest.OperationSpec = {
  httpMethod: "GET",
  path: "v2/publish/{endpointId}",
  urlParameters: [
    Parameters.endpointId
  ],
  queryParameters: [
    Parameters.continuationToken
  ],
  contentType: "application/json; charset=utf-8",
  responses: {
    200: {
      bodyMapper: Mappers.PublishedItemListResponseApiModel
    },
    default: {}
  },
  serializer
};

export {
  AzureOpcPublisherClient,
  AzureOpcPublisherClientContext,
  Models as AzureOpcPublisherModels,
  Mappers as AzureOpcPublisherMappers
};
