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

    public enum EnabledLoggersEnum
    {
        None,
        File,
        Console,
        Debug
    }

    public enum ObjectSerializationFormat
    {
        String,
        Json
    }

    public interface IApplicationSetupConfiguration
    {
        /// <summary>
        /// When objects are being serialized for logging, the format can be:
        ///     String
        ///     Json
        /// </summary>
        ObjectSerializationFormat SerializationFormat { get; set; }

        /// <summary>
        /// Specifies the name of the key vault key we want to use for configuration.
        /// If this contains a non-null value, then Key will be used to access Azure KeyVault.
        /// The data returned will override all the values in "ApplicationSecrets". This
        /// is where all secret/sensitive password/connection information is stored.
        /// The format of this key should be:
        ///         KeyName-Environment
        /// e.g. ReloAccessSecrets-DFX
        ///
        /// Every environment should have InitialConfiguration__KeyVaultKey environment variable
        /// set for their local setup. This way appsettings.json can leave this empty
        /// and every environment in which the code runs will take the value from the
        /// InitialConfiguration__KeyVaultKey environment variable.
        /// </summary>
        string KeyVaultKey { get; set; }

        /// <summary>
        /// Specifies the name of the Azure key vault we want to use to pull the key from
        /// </summary>
        string KeyVaultName { get; set; }

        /// <summary>
        /// If the name of the logger was specified in the configuration
        /// then true is returned
        /// </summary>
        bool IsLoggerEnabled(EnabledLoggersEnum LoggerType);

        /// <summary>
        /// Returns true if any kind of logging is enabled
        /// </summary>
        /// <returns>true or false</returns>
        bool IsLoggingEnabled { get; }


        /// <summary>
        /// Allows you to specify which loggers (if any) are to be used at runtime.
        /// The available options are  "File", "Console", "Debug", "None". If "None"
        /// is specified or no option is provided at all, then no logging will occur.
        /// Otherwise one or more of the options  "File", "Console", "Debug" can be used
        /// together. "Console" logs to the console window. "Debug" logs to the visual studio
        /// debug output window. "File" logs to a file. 
        /// </summary>
        List<string> EnabledLoggers { get; set; }
    }

    public class InitialConfiguration: IApplicationSetupConfiguration
    {
        /// <summary>
        /// When objects are being serialized for logging, the format can be:
        ///     String
        ///     Json
        /// </summary>
        public ObjectSerializationFormat SerializationFormat { get; set; }

        /// <summary>
        /// Specifies the name of the key vault key we want to use for configuration.
        /// If this contains a non-null value, then Key will be used to access Azure KeyVault.
        /// The data returned will override all the values in "ApplicationSecrets". This
        /// is where all secret/sensitive password/connection information is stored.
        /// The format of this key should be:
        ///         KeyName-Environment
        /// e.g. ReloAccessSecrets-DFX
        ///
        /// Every environment should have InitialConfiguration__KeyVaultKey environment variable
        /// set for their local setup. This way appsettings.json can leave this empty
        /// and every environment in which the code runs will take the value from the
        /// InitialConfiguration__KeyVaultKey environment variable.
        /// </summary>
        public string KeyVaultKey { get; set; }

        /// <summary>
        /// Specifies the name of the Azure key vault we want to use to pull the key from
        /// </summary>
        public string KeyVaultName { get; set; }

        /// <summary>
        /// If the name of the logger was specified in the configuration
        /// then true is returned
        /// </summary>
        public bool IsLoggerEnabled(EnabledLoggersEnum LoggerType)
        {
            if (EnabledLoggers != null)
            {
                string found = EnabledLoggers.Find(loggerName => loggerName == LoggerType.ToString());
                return (found != null);
            }

            return false;

        }

        /// <summary>
        /// Returns true if any kind of logging is enabled
        /// </summary>
        /// <returns>true or false</returns>
        public bool IsLoggingEnabled
        {
            get
            {
                bool enabled = true;
                if (EnabledLoggers != null && EnabledLoggers.Count() > 0)
                {
                    if (IsLoggerEnabled(EnabledLoggersEnum.None))
                        enabled = false;
                }

                return (enabled);
            }
        }

        /// <summary>
        /// Allows you to specify which loggers (if any) are to be used at runtime.
        /// The available options are  "File", "Console", "Debug", "None". If "None"
        /// is specified or no option is provided at all, then no logging will occur.
        /// Otherwise one or more of the options  "File", "Console", "Debug" can be used
        /// together. "Console" logs to the console window. "Debug" logs to the visual studio
        /// debug output window. "File" logs to a file. 
        /// </summary>
        public List<string> EnabledLoggers { get; set; }
    }


    /// <summary>
    /// Interface used to access all properties in the "ApplicationSecrets" property of the appsettings.json file
    /// </summary>
    public interface IApplicationSecrets
    {
        /// <summary>
        /// Name of user using the application 
        /// </summary>
        string UserName { get; set; }


        /// <summary>
        /// Returns the connection string associated with the "ConnectionName"
        /// </summary>
        /// <param name="ConnectionName">Name of connection we want to get the connection string for</param>
        /// <returns>Connection string associated with the specified item</returns>
        string ConnectionString(string ConnectionName);

        /// <summary>
        /// Retrieves all the information related to the ConnectionName specified
        /// </summary>
        /// <param name="ConnectionName">Name of the secret/connection you want to get information for</param>
        /// <returns>ApplicationSecretsConnectionStrings</returns>
        IApplicationSecretsConnectionStrings Secret(string ConnectionName);
    }

    public class ApplicationSecrets : IApplicationSecrets
    {
        /// <summary>
        /// Name of user using the application 
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Every Name:Value pair in the MyProjectSettings:ConnectionStrings
        /// appsettings is deserialized into this list.
        /// </summary
        public List<ApplicationSecretsConnectionStrings> ConnectionStrings { get; set; }

        /// <summary>
        /// Returns the connection string associated with the "ConnectionName"
        /// </summary>
        /// <param name="ConnectionName">Name of item we want to get the connection string for</param>
        /// <returns>Connection string associated with the specified item</returns>
        public string ConnectionString(string ConnectionName)
        {
            ApplicationSecretsConnectionStrings found = ConnectionStrings?.Find(item => item.Name == ConnectionName);
            return (found?.Value);
        }

        /// <summary>
        /// Retrieves all the information related to the ConnectionName specified
        /// </summary>
        /// <param name="ConnectionName">Name of the secret/connection you want to get information for</param>
        /// <returns>ApplicationSecretsConnectionStrings</returns>
        public IApplicationSecretsConnectionStrings Secret(string ConnectionName)
        {
            return ConnectionStrings?.Find(item => item.Name == ConnectionName);
        }
    }

    public interface IApplicationSecretsConnectionStrings
    {
        /// <summary>
        /// Name of the database
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Connection string value
        /// </summary>
        string Value { get; set; }

        /// <summary>
        /// Specifies the category of connection string stored in the "Value" property
        /// </summary>
        String Category { get; set; }

        /// <summary>
        /// Describes the purpose of this connection string
        /// </summary>
        string Description { get; set; }

        /// <summary>
        /// Custom metadata associated with the connection string. Can be in any format
        /// that makes sense for your connection.
        /// </summary>
        string Metadata { get; set; }
    }

    /// <summary>
    /// Each connection string entry in the appsettings.json file is represented by a Json object
    /// that has the properties "Name" and "Value". The configuration file has a property called 
    /// "ConnectionStrings" that contains a JSON array of these items.
    /// </summary>
    public class ApplicationSecretsConnectionStrings : IApplicationSecretsConnectionStrings
    {
        /// <summary>
        /// Name of the database
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Connection string value
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Specifies the category of connection string stored in the "Value" property
        /// </summary>
        public String Category { get; set; }

        /// <summary>
        /// Describes the purpose of this connection string
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Custom metadata associated with the connection string. Can be in any format
        /// that makes sense for your connection.
        /// </summary>
        public string Metadata { get; set; }
    }


    public interface IAppConfigSections
    {
        IApplicationSecrets appSecrets { get; set; }
        IApplicationSetupConfiguration appIntialConfig { get; set; }
    }

    public class AppConfigSections : IAppConfigSections
    {
        public IApplicationSecrets appSecrets { get; set; }
        public IApplicationSetupConfiguration appIntialConfig { get; set; }
    }


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

                if (!string.IsNullOrEmpty(_appSetupConfig.KeyVaultName))
                {
                    builder.AddAzureKeyVault(_appSetupConfig.KeyVaultName);
                }

                // Build the final configuration
                _baseConfiguration = builder.Build();

                // Set the IApplicationSecrets from the KeyVault if found, otherwise
                // grab it from appsettings.json/secrets.json
                retVal.appSecrets = InitializeApplicationSecrets(_baseConfiguration, _appSetupConfig);
                retVal.appIntialConfig = _appSetupConfig;

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
