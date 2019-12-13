// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Services {
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Engine;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Tests;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Runtime;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Services;
    using Microsoft.Azure.IIoT.OpcUa.Publisher;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Testing.Fixtures;
    using Microsoft.Azure.IIoT.Agent.Framework;
    using Moq;
    using Autofac.Extras.Moq;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading.Tasks;
    using Xunit;
    using System.Threading;
    using Serilog;

    [Collection(ReadCollection.Name)]
    public class MonitoredItemMessageTests {

        public MonitoredItemMessageTests(TestServerFixture server) {
            _server = server;
        }

        private readonly TestServerFixture _server;

        [Fact]
        public void NodePublishServerTimeTest() {
            using (var mock = AutoMock.GetLoose()) {
                // Setup
                var queue = Setup(mock, _server.Logger, CreateVariablePublishJob(
                    new EndpointModel {
                        Url = $"opc.tcp://{Dns.GetHostName()}:{_server.Port}/UA/SampleServer",
                        Certificate = _server.Certificate?.RawData
                    },
                    new List<PublishedDataSetVariableModel> {
                        new PublishedDataSetVariableModel {
                            PublishedVariableNodeId = "i=2258"
                        }
                    }));

                using (var ct = new CancellationTokenSource()) {
                    var trig = mock.Create<IMessageTrigger>();
                    var run = trig.RunAsync(ct.Token);


                    // Act - read messages from sink
                    var result = queue.TryTake(out var message, 60000);

                    // Assert
                    Assert.True(result);
                    Assert.NotNull(message);


                    ct.Cancel();
                    run.Wait();
                }
            }
        }

        /// <summary>
        /// Setup mock
        /// </summary>
        private static BlockingCollection<NetworkMessageModel> Setup(AutoMock mock,
            ILogger logger, WriterGroupJobModel job) {
            var queue = new BlockingCollection<NetworkMessageModel>();

            mock.Provide(logger);
            mock.Provide<IClientServicesConfig, ClientServicesConfig>();
            mock.Provide<IEndpointServices, ClientServices>();
            mock.Provide<ISubscriptionManager, SubscriptionServices>();
            mock.Provide<IWriterGroupConfig>(new MockJobConfig(job));
            mock.Provide<IMessageTrigger, WriterGroupMessageTrigger>();
            mock.Provide<IMessageEncoder, MonitoredItemMessageEncoder>();
            mock.Provide<IProcessingEngine, DataFlowProcessingEngine>();
            mock.Mock<IMessageSink>()
                .Setup(s => s.SendAsync(It.IsNotNull<IEnumerable<NetworkMessageModel>>()))
                .Returns(Task.CompletedTask)
                .Callback<IEnumerable<NetworkMessageModel>>(m => {
                    foreach (var item in m) {
                        queue.TryAdd(item);
                    }
                });
            return queue;
        }

        /// <summary>
        /// Create job
        /// </summary>
        /// <param name="variables"></param>
        /// <returns></returns>
        private static WriterGroupJobModel CreateVariablePublishJob(EndpointModel endpoint,
            List<PublishedDataSetVariableModel> variables) {
            return new WriterGroupJobModel {
                WriterGroup = new WriterGroupModel {
                    DataSetWriters = new List<DataSetWriterModel> {
                        new DataSetWriterModel {
                            DataSetWriterId = "Test",
                            DataSet = new PublishedDataSetModel {
                                DataSetSource = new PublishedDataSetSourceModel {
                                    Connection = new ConnectionModel {
                                        Endpoint = endpoint
                                    },
                                    PublishedVariables = new PublishedDataItemsModel {
                                        PublishedData = variables
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }

        /// <summary>
        /// Mock job configuration
        /// </summary>
        private class MockJobConfig : IWriterGroupConfig {
            public MockJobConfig(WriterGroupJobModel job) {
                WriterGroup = job.WriterGroup;
            }
            public string PublisherId => "Test";
            public WriterGroupModel WriterGroup { get; }
        }
    }
}
