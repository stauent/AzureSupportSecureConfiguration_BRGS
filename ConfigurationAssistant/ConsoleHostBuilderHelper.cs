using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using System;
using System.IO;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration.AzureKeyVault;
using Microsoft.Extensions.Logging.ApplicationInsights;


namespace ConfigurationAssistant
{
    /// <summary>
    /// Details on how the app "TApp" was created and configured
    /// </summary>
    /// <typeparam name="TApp">The type of the application that was configured</typeparam>
    public class ConfigurationResults<TApp> where TApp : class 
    {
        public IHostBuilder builder { get; set; }
        public IHost myHost { get; set; }

        public TApp myService { get; set; }

        public ST GetService<ST>()
        {
            return myHost.Services.GetService<ST>();
        }
    }

    /// <summary>
    /// This class will configure a console application to use Dependency Injection and support console and debug logging
    /// </summary>
    public static class ConsoleHostBuilderHelper
    {
        public delegate void ConfigureLocalServices<T>(HostBuilderContext hostingContext, IServiceCollection services, IAppConfigSections sections) where T : class;
        /// <summary>
        /// "https://<VAULT_NAME>.vault.azure.net/";
        /// </summary>
        /// <returns></returns>
        private static string GetKeyVaultEndpoint(string KeyVaultName) => $"https://{KeyVaultName}.vault.azure.net/"; 

        /// <summary>
        /// This method will create an initialize a generic Host Builder 
        /// </summary>
        /// <typeparam name="TApp">Main application type. Used to access user secrets</typeparam>
        /// <param name="args">Application arguments</param>
        /// <param name="localServiceConfiguration">Delegate to be executed to add any non-standard configuration needs</param>
        /// <returns>Configured IHostBuilder</returns>
        public static IHostBuilder CreateHostBuilder<TApp>(string[] args, ConfigureLocalServices<TApp> localServiceConfiguration = null) where TApp : class
        {
            IApplicationSecrets appSecrets = null;
            IApplicationSetupConfiguration appIntialConfig = null;
            IAppConfigSections sections = null;

            IHostBuilder hostBuilder =  Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, builder) =>
                {
                    sections = ConfigFactory.Initialize<TApp>(hostingContext,builder);
                    appSecrets = sections.appSecrets;
                    appIntialConfig = sections.appIntialConfig;
                })
                .ConfigureServices((hostingContext, services) =>
                {
                    localServiceConfiguration?.Invoke(hostingContext, services, sections);

                    services
                        .AddTransient<TApp>()
                        .AddSingleton<IApplicationSetupConfiguration>(sp =>
                        {
                            return (appIntialConfig);
                        })
                        .AddSingleton<IApplicationSecrets>(sp =>
                        {
                            return (appSecrets);
                        })
                        .AddSingleton<IHostEnvironment>(sp =>
                        {
                            return (hostingContext.HostingEnvironment);
                        })
                        .AddSingleton<IApplicationRequirements<TApp>, ApplicationRequirements<TApp>>();

                    services.BuildServiceProvider();
                })
                .ConfigureLogging((hostingContext, logging) =>
                {
                    ConfigureCustomLogging(hostingContext, logging, appSecrets, appIntialConfig);
                });

            return (hostBuilder);
        }

        /// <summary>
        /// Adds Azure KeyVault as part of the app configuration
        /// </summary>
        /// <param name="builder">IConfigurationBuilder to build up the configuration</param>
        /// <param name="KeyVaultName">Name of the key vault to connect to</param>
        public static void AddAzureKeyVault(this IConfigurationBuilder builder, string KeyVaultName)
        {
            try
            {
                if (!string.IsNullOrEmpty(KeyVaultName))
                {
                    var keyVaultEndpoint = GetKeyVaultEndpoint(KeyVaultName);
                    if (!string.IsNullOrEmpty(keyVaultEndpoint))
                    {
                        var azureServiceTokenProvider = new AzureServiceTokenProvider();
                        var keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));
                        builder.AddAzureKeyVault(keyVaultEndpoint, keyVaultClient, new DefaultKeyVaultSecretManager());
                    }
                }
            }
            catch (Exception e)
            {
            }
        }

        /// <summary>
        /// Different types of logging are enabled based on the MyProjectSettings:EnabledLoggers: [ "File", "Console", "Debug" ]
        /// </summary>
        /// <param name="hostingContext">Generic host builder context used to configure the application</param>
        /// <param name="logging">Interface used to configure logging providers</param>
        /// <param name="applicationSecrets">Interface used to access all properties in the "ApplicationSecrets" property of the appsettings.json file</param>
        /// <param name="applicationSetupConfiguration">Interface used to access all properties in the "InitialConfiguration" property of the appsettings.json file</param>
        public static void ConfigureCustomLogging(HostBuilderContext hostingContext, ILoggingBuilder logging, IApplicationSecrets applicationSecrets, IApplicationSetupConfiguration applicationSetupConfiguration)
        {
            logging.ClearProviders();

            if (applicationSetupConfiguration.IsLoggingEnabled)
            {
                logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));

                if (applicationSetupConfiguration.IsLoggerEnabled(EnabledLoggersEnum.Debug))
                    logging.AddDebug();

                if (applicationSetupConfiguration.IsLoggerEnabled(EnabledLoggersEnum.Console))
                    logging.AddConsole();

                if (applicationSetupConfiguration.IsLoggerEnabled(EnabledLoggersEnum.File))
                {
                    // The FileLogger will be configured for log4Net local logging during development.
                    // It will contain the instrumentation key for Azure Application Insights when
                    // the connection information is coming from KeyVault
                    string logConnectionString = applicationSecrets.ConnectionString("FileLogger");
                    Guid gKey;
                    if (!string.IsNullOrEmpty(applicationSetupConfiguration.KeyVaultKey) && Guid.TryParse(logConnectionString, out gKey))
                    {
                        string instrumentationKey = logConnectionString;
                        logging.AddApplicationInsights(instrumentationKey);

                        // Adding the filter below to ensure logs of all severities 
                        // is sent to ApplicationInsights.
                        logging.AddFilter<Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider>
                                ("RemoteLogging", LogLevel.Debug);
                    }
                    else  // No local KeyVaultKey or environment setting for InitialConfiguration_KeyVaultKey found, so use local Log4Net logging
                    {
                        // Must set the log name prior to adding Log4Net because it must know this value
                        // before it loads the config file. It does pattern matching and substitution on the filename.
                        string logPath = null;
                        string logName = null;
                        if (!string.IsNullOrEmpty(logConnectionString))
                        {
                            string[] logParts = logConnectionString.Split(";");
                            logPath = logParts[0]?.Replace("LogPath=", "");
                            logName = logParts[1]?.Replace("LogName=", "");
                        }

                        if (!string.IsNullOrEmpty(logPath))
                        {
                            if (!Directory.Exists(logPath))
                            {
                                Directory.CreateDirectory(logPath);
                            }

                            logName = $"{logPath}\\{logName}";
                        }

                        log4net.GlobalContext.Properties["LogName"] = logName;
                        logging.AddLog4Net("log4net.config");
                    }
                }
            }
        }

        /// <summary>
        /// Creates IOC container for console apps. Injects logging and custom configuration
        /// for application to consume in its constructor. The following example shows how
        /// to launch a class called "MyApplication" as your main application. It's constructor
        /// will have logging and configuration injected into it.
        ///
        ///             configuredApplication = ConsoleHostBuilderHelper.CreateApp<MyApplication>(args);
        ///             await configuredApplication.myService.Run();
        /// 
        /// </summary>
        /// <typeparam name="TApp">Type of your main application class</typeparam>
        /// <param name="args">Any command line parameters you used to launch the console app are passed here</param>
        /// <param name="localServiceConfiguration">Delegate you can use to add more services to the IOC container</param>
        /// <returns>An ConfigurationResults object containing all the information about how this application is hosted</returns>
        public static ConfigurationResults<TApp> CreateApp<TApp>(string[] args, ConfigureLocalServices<TApp> localServiceConfiguration = null) where TApp : class
        {
            ConfigurationResults<TApp> config = new ConfigurationResults<TApp>();
            config.builder = CreateHostBuilder<TApp>(args, localServiceConfiguration);
            config.myHost = config.builder.Build();
            config.myService = config.myHost.Services.GetRequiredService<TApp>();
            return (config);
        }

        public static IApplicationRequirements<TApp> CreateApplicationRequirements<TApp>(string[] args, ConfigureLocalServices<TApp> localServiceConfiguration = null) where TApp : class
        {
            ConfigurationResults<TApp> config = new ConfigurationResults<TApp>();
            config.builder = CreateHostBuilder<TApp>(args, localServiceConfiguration);
            config.myHost = config.builder.Build();
            IApplicationRequirements<TApp> requirements = config.myHost.Services.GetRequiredService<IApplicationRequirements<TApp>>();
            return (requirements);
        }
    }
}
