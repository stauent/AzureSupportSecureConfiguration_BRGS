using System;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ConfigurationAssistant
{
    /// <summary>
    /// Every interface that the application needs is provided in a
    /// single interface that exposes each interface as a property.
    /// This way, all constructors simply have ONE parameter of type
    /// IApplicationRequirements, and every dependency injected interface you need
    /// is supplied for you. You don't need to specify each one
    /// individually in your constructor.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IApplicationRequirements<T>
    {
        ILogger<T> ApplicationLogger { get; set; }
        IApplicationSecrets applicationSecrets { get; set; }
        IConfiguration ApplicationConfiguration { get; set; }
        IApplicationSetupConfiguration ApplicationSetupConfiguration { get; set; }
    }

    /// <summary>
    /// Every constructor that needs application required interfaces
    /// should have IApplicationRequirements as a parameter. This
    /// one interface will provide the app with every other interface it needs. 
    /// </summary>
    /// <typeparam name="T">Type of the application that is running. Provides a mechanism to write logs to a specific filename</typeparam>
    public class ApplicationRequirements<T> : IApplicationRequirements<T>
    {
        public ILogger<T> ApplicationLogger { get; set; }
        public IApplicationSecrets applicationSecrets { get; set; }
        public IConfiguration ApplicationConfiguration { get; set; }
        public IApplicationSetupConfiguration ApplicationSetupConfiguration { get; set; }

        public ApplicationRequirements(ILogger<T> applicationLogger, IApplicationSecrets applicationSecrets, IConfiguration applicationConfiguration, IApplicationSetupConfiguration applicationSetupConfiguration)
        {
            try
            {
                this.applicationSecrets = applicationSecrets;
                this.ApplicationLogger = applicationLogger;
                this.ApplicationConfiguration = applicationConfiguration;
                this.ApplicationSetupConfiguration = applicationSetupConfiguration;
                TraceLoggerExtension._Logger = applicationLogger;
                TraceLoggerExtension._SerializationFormat = applicationSetupConfiguration.SerializationFormat;
            }
            catch
            {
            }
        }
    }

    /// <summary>
    /// Extension method that will allow ANY object to log it's information
    /// with a very simple syntax. Just append one of the ".TraceXXX" methods
    /// to ANY object, and the contents of that object will be output in the
    /// specified log locations.
    /// </summary>
    public static class TraceLoggerExtension
    {
        private static ILogger _logger = null;
        public static ILogger _Logger
        {
            get { return (_logger);}
            set
            {
                if (_logger == null)
                    _logger = value;
            }
        }

        public static ObjectSerializationFormat _SerializationFormat { get; set; } = ObjectSerializationFormat.Json;

        public static void TraceInformation(this object objectToTrace, string message = null, [CallerLineNumber] int LineNumber = 0, [CallerMemberName] string MethodName = null, [CallerFilePath] string FileName = null)
        {
            _Logger?.LogInformation($"\r\n\t{ExtractFileName(FileName)}:{MethodName}:{LineNumber} {message ?? ""}\r\n\t{ConvertToString(objectToTrace)}");
        }
        public static void TraceCritical(this object objectToTrace, string message = null, [CallerLineNumber] int LineNumber = 0, [CallerMemberName] string MethodName = null, [CallerFilePath] string FileName = null)
        {
            _Logger?.LogCritical($"\r\n\t{ExtractFileName(FileName)}:{MethodName}:{LineNumber} {message ?? ""}\r\n\t{ConvertToString(objectToTrace)}");
        }
        public static void TraceDebug(this object objectToTrace, string message = null, [CallerLineNumber] int LineNumber = 0, [CallerMemberName] string MethodName = null, [CallerFilePath] string FileName = null)
        {
            _Logger?.LogDebug($"\r\n\t{ExtractFileName(FileName)}:{MethodName}:{LineNumber} {message ?? ""}\r\n\t{ConvertToString(objectToTrace)}");
        }
        public static void TraceError(this object objectToTrace, string message = null, [CallerLineNumber] int LineNumber = 0, [CallerMemberName] string MethodName = null, [CallerFilePath] string FileName = null)
        {
            _Logger?.LogError($"\r\n\t{ExtractFileName(FileName)}:{MethodName}:{LineNumber} {message ?? ""}\r\n\t{ConvertToString(objectToTrace)}");
        }
        public static void TraceWarning(this object objectToTrace, string message = null, [CallerLineNumber] int LineNumber = 0, [CallerMemberName] string MethodName = null, [CallerFilePath] string FileName = null)
        {
            _Logger?.LogWarning($"\r\n\t{ExtractFileName(FileName)}:{MethodName}:{LineNumber} {message ?? ""}\r\n\t{ConvertToString(objectToTrace)}");
        }

        public static string ExtractFileName(string FilePath)
        {
            string retVal = FilePath;

            try
            {
                if (!string.IsNullOrEmpty(FilePath))
                {
                    string[] parts = FilePath.Split("\\");
                    retVal = parts[parts.Length - 1];
                }
            }
            catch 
            {
            }
            return (retVal);
        }

        static string ConvertToString(object objectToTrace)
        {
            string retVal = "";
            if (objectToTrace != null)
            {
                JsonSerializerSettings jSettings = new JsonSerializerSettings()
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    MaxDepth = 1
                };

                if (objectToTrace != null)
                {
                    switch (_SerializationFormat)
                    {
                        case ObjectSerializationFormat.Json:
                            retVal = JsonConvert.SerializeObject(objectToTrace, Formatting.Indented, jSettings);
                            break;
                        case ObjectSerializationFormat.String:
                            retVal = retVal.ToString();
                            break;
                    }
                }
            }

            return (retVal);
        }
    }
}
