# coding=utf-8
# --------------------------------------------------------------------------
# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License. See License.txt in the project root for
# license information.
#
# Code generated by Microsoft (R) AutoRest Code Generator 2.3.33.0
# Changes may cause incorrect behavior and will be lost if the code is
# regenerated.
# --------------------------------------------------------------------------

from msrest.serialization import Model


class PublishStartResponseApiModel(Model):
    """Result of publish request.

    :param error_info:
    :type error_info: ~azure-iiot-opc-publisher.models.ServiceResultApiModel
    """

    _attribute_map = {
        'error_info': {'key': 'errorInfo', 'type': 'ServiceResultApiModel'},
    }

    def __init__(self, error_info=None):
        super(PublishStartResponseApiModel, self).__init__()
        self.error_info = error_info
