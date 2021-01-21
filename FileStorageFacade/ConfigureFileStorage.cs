using Microsoft.Extensions.DependencyInjection;

namespace FileStorageFacade
{
    /// <summary>
    /// Used to set up IOC container with appropriate storage class based on configuration needs
    /// </summary>
    public static class ConfigureFileStorage
    {
        /// <summary>
        /// Initializes the IFileStorageFacade interface for used application use 
        /// </summary>
        /// <param name="UseAzureBlobStorage">True if we are to use Azure blob storage</param>
        /// <param name="services">IServiceCollection where we can register the IFileStorageFacade service</param>
        public static void Initialize(bool UseAzureBlobStorage, IServiceCollection services)
        {
            if (UseAzureBlobStorage)
            {
                services.AddTransient<IFileStorageFacade, BlobFileStorage>();
            }
            else
            {
                services.AddTransient<IFileStorageFacade, LocalFileStorage>();
            }
        }
    }
}
