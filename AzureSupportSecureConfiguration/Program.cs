using System;
using System.Collections.Generic;
using ConfigurationAssistant;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using FileStorageFacade;
using ServiceBusFacade;
using CacheFacade;

namespace AzureSupportSecureConfiguration
{
    class Program
    {
        public static ConfigurationResults<MyApplication> configuredApplication = null;

        static async Task Main(string[] args)
        {
            // Configure the application. Set up IOC container with services required at runtime.
            configuredApplication = ConsoleHostBuilderHelper.CreateApp<MyApplication>(args, ConfigureLocalServices);
            await configuredApplication.myService.Run();
        }

        /// <summary>
        /// If the default factory method "ConsoleHostBuilderHelper.CreateApp" does not support all the services
        /// you need at runtime, then you can add them here. "CreateApp" calls this method before any other services
        /// are added to the IServiceCollection.
        /// </summary>
        /// <param name="hostingContext"></param>
        /// <param name="services"></param>
        public static void ConfigureLocalServices(HostBuilderContext hostingContext, IServiceCollection services, IAppConfigSections sections)
        {
            // Register the appropriate interfaces depending on the environment
            bool useKeyVaultKey = !string.IsNullOrEmpty(sections.appIntialConfig.KeyVaultKey);

            ConfigureFileStorage.Initialize(useKeyVaultKey, services);
            ConfigureServiceBus.Initialize(useKeyVaultKey, services);
            ConfigureApplicationCache.Initialize(useKeyVaultKey, services);
        }
    }
}
