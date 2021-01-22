using ConfigurationAssistant;


namespace CacheFacade
{
    public class CacheBase
    {
        /// <summary>
        /// Provides the name of the configuration file that is used to configure the cache
        /// </summary>
        public static readonly string ConfigSectionName = "ApplicationCache";

        public static string CacheConnectionString { get; private set; }

        protected readonly IApplicationSecrets _applicationSecrets;

        public CacheBase(IApplicationSecrets applicationSecrets)
        {
            _applicationSecrets = applicationSecrets;
            CacheConnectionString = _applicationSecrets.ConnectionString(CacheBase.ConfigSectionName);
        }
    }
}
