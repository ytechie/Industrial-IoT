/*
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for
 * license information.
 *
 * Code generated by Microsoft (R) AutoRest Code Generator 1.0.0.0
 * Changes may cause incorrect behavior and will be lost if the code is
 * regenerated.
 */

'use strict';

/**
 * History read continuation result
 *
 */
class HistoricValueApiModelHistoryReadNextResponseApiModel {
  /**
   * Create a HistoricValueApiModelHistoryReadNextResponseApiModel.
   * @property {array} [history] History as json encoded extension object
   * @property {string} [continuationToken] Continuation token if more results
   * pending.
   * @property {object} [errorInfo]
   * @property {number} [errorInfo.statusCode] Error code - if null operation
   * succeeded.
   * @property {string} [errorInfo.errorMessage] Error message in case of error
   * or null.
   * @property {object} [errorInfo.diagnostics] Additional diagnostics
   * information
   */
  constructor() {
  }

  /**
   * Defines the metadata of HistoricValueApiModelHistoryReadNextResponseApiModel
   *
   * @returns {object} metadata of HistoricValueApiModelHistoryReadNextResponseApiModel
   *
   */
  mapper() {
    return {
      required: false,
      serializedName: 'HistoricValueApiModel_HistoryReadNextResponseApiModel',
      type: {
        name: 'Composite',
        className: 'HistoricValueApiModelHistoryReadNextResponseApiModel',
        modelProperties: {
          history: {
            required: false,
            serializedName: 'history',
            type: {
              name: 'Sequence',
              element: {
                  required: false,
                  serializedName: 'HistoricValueApiModelElementType',
                  type: {
                    name: 'Composite',
                    className: 'HistoricValueApiModel'
                  }
              }
            }
          },
          continuationToken: {
            required: false,
            serializedName: 'continuationToken',
            type: {
              name: 'String'
            }
          },
          errorInfo: {
            required: false,
            serializedName: 'errorInfo',
            type: {
              name: 'Composite',
              className: 'ServiceResultApiModel'
            }
          }
        }
      }
    };
  }
}

module.exports = HistoricValueApiModelHistoryReadNextResponseApiModel;
