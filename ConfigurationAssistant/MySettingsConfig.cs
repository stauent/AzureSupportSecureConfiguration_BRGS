using Microsoft.Extensions.Configuration;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ConfigurationAssistant
{

    // This application uses user-secrets to hide configuration settings.
    // Values that are NOT secret can be stored in the appsettings.json file in the open.
    // Values that ARE SECRET and should not be known to anyone are stored in a user-secret. Read the following
    // article to understand the process.
    // How to use user-secrets https://www.infoworld.com/article/3576292/how-to-work-with-user-secrets-in-asp-net-core.html
    // HMAC authentication: https://bitoftech.net/2014/12/15/secure-asp-net-web-api-using-api-key-authentication-hmac-authentication/
    public static class ConfigFactory
    {
        private static object locker = new object();
        private static Dictionary<string, IAppConfigSections> _userConfiguration = new Dictionary<string, IAppConfigSections>();

        private static void AddUserConfiguration(string AssemblyName, IAppConfigSections UserConfiguration)
        {
            lock (locker)
            {
                // Ensure key doesn't already exist
                if (!_userConfiguration.ContainsKey(AssemblyName))
                    _userConfiguration.Add(AssemblyName, UserConfiguration);
            }
        }

        private static IAppConfigSections FindConfiguration(string AssemblyName)
        {
            IAppConfigSections userConfiguration = null;
            lock (locker)
            {
                if (_userConfiguration.ContainsKey(AssemblyName))
                    userConfiguration = _userConfiguration[AssemblyName];
            }

            return (userConfiguration);
        }

        /// <summary>
        /// Maintains reference to base configuration interface 
        /// </summary>
        public static IConfigurationRoot baseConfiguration {
            get { return(_baseConfiguration); }
        }
        private static IConfigurationRoot _baseConfiguration { get; set; }

        /// <summary>
        /// Maintains reference to InitialConfiguration interface 
        /// </summary>
        public static IApplicationSetupConfiguration appSetupConfig
        {
            get { return (_appSetupConfig); }
        }
        private static IApplicationSetupConfiguration _appSetupConfig { get; set; }
        private static IRedisCache _cache { get; set; }

        /// <summary>
        /// Provides the user the opportunity to initialize the user configuration
        /// based on a type T that exists in the same assembly as where the user secret
        /// "UserSecretsId" section exists in the .csproj file
        /// </summary>
        /// <typeparam name="T">A type that exists in the user secret assembly</typeparam>
        /// <returns>Configured IApplicationSecrets</returns>
        public static IAppConfigSections Initialize<T>(HostBuilderContext context, IConfigurationBuilder builder) where T : class
        {
            Assembly CurrentAssembly = typeof(T).GetTypeInfo().Assembly;
            return (Initialize(context, builder,CurrentAssembly));
        }

        /// <summary>
        /// Provides the user the opportunity to initialize the user configuration
        /// </summary>
        /// <param name="context">HostBuilderContext</param>
        /// <param name="builder">IConfigurationBuilder</param>
        /// <param name="CurrentAssembly">The assembly where the "UserSecretsId" exists in the .csproj file</param>
        /// <returns>IAppConfigSections</returns>
        public static IAppConfigSections Initialize(HostBuilderContext context, IConfigurationBuilder builder, Assembly CurrentAssembly = null)
        {
            IAppConfigSections retVal = null;

            // If the user did not specify the assembly that contains the "UserSecretsId" configuration
            // then assume its the entry assembly
            if (CurrentAssembly == null)
                CurrentAssembly = Assembly.GetEntryAssembly();

            string AssemblyName = CurrentAssembly.FullName;
            retVal = FindConfiguration(AssemblyName);
            if (retVal == null)
            {
                retVal = new AppConfigSections();

                // NOTE: The order in which we add to the configuration builder will
                //       determine the order of override. So in this case the settings
                //       in the "appsettings.json" file are used first, if a user-secret
                //       with the same name is provided then it will override the value
                //       in the .json file. And finally, if an environment variable
                //       with the same name is found then it will override the user-secret.
                if (builder == null)
                    builder = new ConfigurationBuilder();

                builder.SetBasePath(Directory.GetCurrentDirectory());

                // Bind the configuration properties to the properties in the SettingsConfig object
                var initialConfig = builder.Build();
                IConfigurationSection myInitialConfig = initialConfig.GetSection(InitialConfigurationSectionName);
                _appSetupConfig = new InitialConfiguration();
                myInitialConfig.Bind(_appSetupConfig);

                // Override appsettings.json properties with user secrets and Azure KeyVault settings.
                try
                {
                    builder.AddUserSecrets(CurrentAssembly);
                }
                catch (Exception Err)
                {
                }

                if (!string.IsNullOrEmpty(_appSetupConfig.KeyVaultName) && !string.IsNullOrEmpty(_appSetupConfig.KeyVaultKey))
                {
                    // Substitute the runtime environment name in the keyvault properties
                    _appSetupConfig.KeyVaultName = _appSetupConfig.KeyVaultName.Replace("{RTE}", _appSetupConfig.RTE);
                    _appSetupConfig.KeyVaultKey = _appSetupConfig.KeyVaultKey.Replace("{RTE}", _appSetupConfig.RTE);

                    builder.AddAzureKeyVault(_appSetupConfig.KeyVaultName);
                }

                // Build the final configuration
                _baseConfiguration = builder.Build();

                // Set the IApplicationSecrets from the KeyVault if found, otherwise
                // grab it from appsettings.json/secrets.json
                retVal.appSecrets = InitializeApplicationSecrets(_baseConfiguration, _appSetupConfig);
                retVal.appIntialConfig = _appSetupConfig;

                // Use the KeyVault secrets connect to redis cache
                _cache = _baseConfiguration.InitializeRedisCache(retVal.appSecrets);

                // Set up automated refresh from redis cache. "TimedCacheRefresh" configuration
                // setting determines which keys are read from the cache and how often they are read.
                // These values are then placed as regular values that can be read from IConfiguration
                _cache?.RefreshConfigurationFromCache(retVal.appSecrets, _baseConfiguration);

                // Save the configuration so we don't have to create it again
                AddUserConfiguration(AssemblyName, retVal);
            }

            return (retVal);
        }

        public static string ApplicationSecretsSectionName { get; set; } = "ApplicationSecrets";
        public static string InitialConfigurationSectionName { get; set; } = "InitialConfiguration";


        /// <summary>
        /// The appsettings section "ApplicationSecrets" contains all connection string and sensitive information.
        /// To hide this information from source control and to allow individual developers to have their own settings
        /// we copy the section "ApplicationSecrets" into the secrets.json file for local development.
        /// In production this value will come from KeyVault. This method reads the appropriate values
        /// can generates the final IApplicationSecrets that will be used at runtime. 
        /// </summary>
        /// <param name="configuration">IConfigurationRoot</param>
        /// <param name="applicationSetupConfiguration">IApplicationSetupConfiguration</param>
        /// <returns>IApplicationSecrets containing contents of the "ApplicationSecrets" section of configuration</returns>
        public static IApplicationSecrets InitializeApplicationSecrets(IConfigurationRoot configuration, IApplicationSetupConfiguration applicationSetupConfiguration)
        {
            ApplicationSecrets retVal = null;

            try
            {
                if (!string.IsNullOrEmpty(applicationSetupConfiguration.KeyVaultKey))
                {
                    string mySecret = configuration[applicationSetupConfiguration.KeyVaultKey];
                    string decoded = Base64Decode(mySecret);

                    JObject jo = JObject.Parse(decoded);
                    string val = jo.Properties().First(x => x.Name == ApplicationSecretsSectionName).Value.ToString();
                    retVal = JsonConvert.DeserializeObject<ApplicationSecrets>(val);
                }
            }
            catch(Exception Err) 
            {
            }

            // Bind the local configuration properties to the properties in the ApplicationSecrets object
            IConfigurationSection myConfiguration = configuration.GetSection(ApplicationSecretsSectionName);
            ApplicationSecrets localSecrets = new ApplicationSecrets();
            myConfiguration.Bind(localSecrets);

            // If the local configuration contains secrets that were not present in KeyVault, then include them
            if (retVal != null)
            {
                foreach (ApplicationSecretsConnectionStrings nextSecret in localSecrets.ConnectionStrings)
                {
                    // Try to find the local secret name in the KeyVault version. If not found in KeyVault, then insert it
                    // into final merge.
                    IApplicationSecretsConnectionStrings found = retVal.Secret(nextSecret.Name);
                    if (found == null)
                    {
                        retVal.ConnectionStrings.Add(nextSecret);
                    }
                }
            }
            else
            {
                retVal = localSecrets;
            }

            return (retVal);
        }

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        public static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }
    }
}
