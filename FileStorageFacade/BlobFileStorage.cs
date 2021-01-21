using System.IO;
using System.Threading.Tasks;

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using ConfigurationAssistant;

namespace FileStorageFacade
{
    /// <summary>
    /// Implements methods that will write to Azure blob storage
    /// </summary>
    public class BlobFileStorage : IFileStorageFacade
    {
        private readonly IApplicationSecrets _applicationSecrets;
        public BlobFileStorage(IApplicationSecrets applicationSecrets)
        {
            _applicationSecrets = applicationSecrets;
        }

        protected BlobContainerClient GetContainerClient()
        {
            // Uses the IApplicationSecrets interface to retrieve all the data related to the secret "BlobStorage"  
            IApplicationSecretsConnectionStrings secret = _applicationSecrets.Secret("BlobStorage");
            string blobConnectionString = secret.Value;
            string containerName = secret.Metadata;

            // Create a BlobServiceClient object which will be used to get a container 
            BlobServiceClient blobServiceClient = new BlobServiceClient(blobConnectionString);

            // Get the container containing the blob
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            return (containerClient);
        }

        public async Task CopyTo(string From, string To)
        {
            // Get the container containing the blob
            BlobContainerClient containerClient = GetContainerClient();

            // Get a reference to a blob
            BlobClient blobClient = containerClient.GetBlobClient(To);

            // Open the file and upload its data
            using (FileStream uploadFileStream = File.OpenRead(From))
            {
                await blobClient.UploadAsync(uploadFileStream, true);
                uploadFileStream.Close();
            }
        }

        public async Task CopyFrom(string To, string From)
        {
            // Get the container containing the blob
            BlobContainerClient containerClient = GetContainerClient();

            // Get a reference to a blob
            BlobClient blobClient = containerClient.GetBlobClient(From);

            // Download the blob's contents and save it to a file
            BlobDownloadInfo download = await blobClient.DownloadAsync();

            using (FileStream downloadFileStream = File.OpenWrite(To))
            {
                await download.Content.CopyToAsync(downloadFileStream);
                downloadFileStream.Close();
            }
        }
    }
}
