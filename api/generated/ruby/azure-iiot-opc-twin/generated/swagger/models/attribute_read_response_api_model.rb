# encoding: utf-8
# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License. See License.txt in the project root for
# license information.
#
# Code generated by Microsoft (R) AutoRest Code Generator 1.0.0.0
# Changes may cause incorrect behavior and will be lost if the code is
# regenerated.

module azure.iiot.opc.twin
  module Models
    #
    # Attribute value read
    #
    class AttributeReadResponseApiModel
      # @return Attribute value
      attr_accessor :value

      # @return [ServiceResultApiModel]
      attr_accessor :error_info


      #
      # Mapper for AttributeReadResponseApiModel class as Ruby Hash.
      # This will be used for serialization/deserialization.
      #
      def self.mapper()
        {
          client_side_validation: true,
          required: false,
          serialized_name: 'AttributeReadResponseApiModel',
          type: {
            name: 'Composite',
            class_name: 'AttributeReadResponseApiModel',
            model_properties: {
              value: {
                client_side_validation: true,
                required: false,
                serialized_name: 'value',
                type: {
                  name: 'Object'
                }
              },
              error_info: {
                client_side_validation: true,
                required: false,
                serialized_name: 'errorInfo',
                type: {
                  name: 'Composite',
                  class_name: 'ServiceResultApiModel'
                }
              }
            }
          }
        }
      end
    end
  end
end
