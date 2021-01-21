using Microsoft.Extensions.DependencyInjection;

namespace ServiceBusFacade
{
    /// <summary>
    /// Used to set up IOC container with appropriate storage class based on configuration needs
    /// </summary>
    public static class ConfigureServiceBus
    {
        /// <summary>
        /// Initializes the IMessageBus interface for used application use 
        /// </summary>
        /// <param name="UseCloud">True if we are to use cloud</param>
        /// <param name="services">IServiceCollection where we can register the IMessageBus service</param>
        public static void Initialize(bool UseCloud, IServiceCollection services)
        {
            if (UseCloud)
            {
                services.AddTransient<IMessageBus, AzureServiceBus>();
            }
            else
            {
                services.AddTransient<IMessageBus, LocalServiceBus>();
            }
        }
    }
}
