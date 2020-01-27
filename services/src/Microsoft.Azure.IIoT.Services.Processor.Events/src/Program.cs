// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Processor.Events {
    using Microsoft.Azure.IIoT.Services.Processor.Events.Runtime;
    using Microsoft.Azure.IIoT.Messaging.SignalR.Services;
    using Microsoft.Azure.IIoT.OpcUa.Api.Onboarding;
    using Microsoft.Azure.IIoT.OpcUa.Api.Onboarding.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Registry;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Handlers;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Services;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Clients;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Messaging.Default;
    using Microsoft.Azure.IIoT.Messaging.ServiceBus.Clients;
    using Microsoft.Azure.IIoT.Messaging.ServiceBus.Services;
    using Microsoft.Azure.IIoT.Hub.Processor.EventHub;
    using Microsoft.Azure.IIoT.Hub.Processor.Services;
    using Microsoft.Azure.IIoT.Hub.Services;
    using Microsoft.Azure.IIoT.Http.Default;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.IIoT.Auth.Clients.Default;
    using Microsoft.Extensions.Configuration;
    using Autofac;
    using Serilog;
    using System;
    using System.IO;
    using System.Runtime.Loader;
    using System.Threading.Tasks;
    using Microsoft.Azure.IIoT.Hub.Client;

    /// <summary>
    /// IoT Hub device events event processor host.  Processes all
    /// events from devices including onboarding and discovery events.
    /// </summary>
    public class Program {

        /// <summary>
        /// Main entry point for iot hub device event processor host
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args) {

            // Load hosting configuration
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", true)
                .AddEnvironmentVariables()
                .AddEnvironmentVariables(EnvironmentVariableTarget.User)
                .AddFromDotEnvFile()
                .AddFromKeyVault()
                .AddCommandLine(args)
                .Build();

            // Set up dependency injection for the event processor host
            RunAsync(config).Wait();
        }

        /// <summary>
        /// Run
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static async Task RunAsync(IConfiguration config) {
            var exit = false;
            while (!exit) {
                // Wait until the event processor host unloads or is cancelled
                var tcs = new TaskCompletionSource<bool>();
                AssemblyLoadContext.Default.Unloading += _ => tcs.TrySetResult(true);
                using (var container = ConfigureContainer(config).Build()) {
                    var logger = container.Resolve<ILogger>();
                    try {
                        logger.Information("Events processor host started.");
                        exit = await tcs.Task;
                    }
                    catch (InvalidConfigurationException e) {
                        logger.Error(e,
                            "Error starting events processor host - exit!");
                        return;
                    }
                    catch (Exception ex) {
                        logger.Error(ex,
                            "Error running events processor host - restarting!");
                    }
                }
            }
        }

        /// <summary>
        /// Autofac configuration.
        /// </summary>
        public static ContainerBuilder ConfigureContainer(
            IConfiguration configuration) {

            var serviceInfo = new ServiceInfo();
            var config = new Config(configuration);
            var builder = new ContainerBuilder();

            builder.RegisterInstance(serviceInfo)
                .AsImplementedInterfaces().SingleInstance();

            // Register configuration interfaces
            builder.RegisterInstance(config)
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterInstance(config.Configuration)
                .AsImplementedInterfaces().SingleInstance();

            // register diagnostics
            builder.AddDiagnostics(config);

            // Event processor services for onboarding consumer
            builder.RegisterType<EventProcessorHost>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<EventProcessorFactory>()
                .AsImplementedInterfaces().SingleInstance();
            // ... and auto start
            builder.RegisterType<HostAutoStart>()
                .AutoActivate()
                .AsImplementedInterfaces().SingleInstance();

            // Handle iot hub telemetry events...
            builder.RegisterType<IoTHubDeviceEventHandler>()
                .AsImplementedInterfaces().SingleInstance();
            // ... and pass to the following handlers:

            // 1.) Handler for discovery events
            builder.RegisterType<DiscoveryEventHandler>()
                .AsImplementedInterfaces().SingleInstance();
            // ... requires the corresponding services
            // Call onboarder
            builder.RegisterType<OnboardingAdapter>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<OnboardingServiceClient>()
                .AsImplementedInterfaces().SingleInstance();
            // using Http client module (needed for api)
            builder.RegisterModule<HttpClientModule>();
            // with Managed or service principal authentication
            builder.RegisterType<AppAuthenticationProvider>()
                .AsImplementedInterfaces().SingleInstance();

            // 2.) Handler for discovery messages
            builder.RegisterType<DiscoveryMessageHandler>()
                .AsImplementedInterfaces().SingleInstance();
            // ... and forward discovery progress to clients
            builder.RegisterType<DiscoveryProgressPublisher>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<SignalRServiceHost>()
                .AsImplementedInterfaces().SingleInstance();

            // 3.) Handler for device change events ...
            builder.RegisterType<IoTHubTwinChangeEventHandler>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<IoTHubDeviceLifecycleEventHandler>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<IoTHubModuleLifecycleEventHandler>()
                .AsImplementedInterfaces().SingleInstance();

            builder.RegisterType<SupervisorTwinEventHandler>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<PublisherTwinEventHandler>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<DiscovererTwinEventHandler>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<GatewayTwinEventHandler>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<EndpointTwinEventHandler>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ApplicationTwinEventHandler>()
                .AsImplementedInterfaces().SingleInstance();

            // ... publish to registered event bus
            builder.RegisterType<EventBusHost>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ServiceBusClientFactory>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ServiceBusEventBus>()
                .AsImplementedInterfaces().SingleInstance();

            // Iot hub services
            builder.RegisterType<IoTHubMessagingHttpClient>()
                .AsImplementedInterfaces().SingleInstance();

            return builder;
        }
    }
}
