
using Microsoft.Extensions.DependencyInjection;

namespace CacheFacade
{
    /// <summary>
    /// Used to set up IOC container with appropriate storage class based on configuration needs
    /// </summary>
    public static class ConfigureApplicationCache
    {
        /// <summary>
        /// Initializes the IMessageBus interface for used application use 
        /// </summary>
        /// <param name="UseCloud">True if we are to use cloud</param>
        /// <param name="services">IServiceCollection where we can register the IApplicationCache service</param>
        public static void Initialize(bool UseCloud, IServiceCollection services)
        {
            if (UseCloud)
            {
                services.AddSingleton<IApplicationCache, RedisCache>() ;
            }
            else
            {
                services.AddMemoryCache();
                services.AddSingleton<IApplicationCache, LocalMemoryCache>();
            }
        }
    }
}
