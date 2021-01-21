using System.IO;
using System.Text;
using System.Threading.Tasks;

using ConfigurationAssistant;

namespace FileStorageFacade
{
    /// <summary>
    /// Implements methods that will write to local file storage
    /// </summary>
    public class LocalFileStorage : IFileStorageFacade
    {
        private readonly IApplicationSecrets _applicationSecrets;
        public LocalFileStorage(IApplicationSecrets applicationSecrets)
        {
            _applicationSecrets = applicationSecrets;
        }
        public async Task CopyTo(string From, string To)
        {
            string[] parts = From.Split("\\");
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < parts.Length - 1; ++i)
            {
                sb.Append(parts[i] + "\\");
            }

            sb.Append(To);

            File.Copy(From, sb.ToString(), true);
            return;
        }

        public async Task CopyFrom(string To, string From)
        {
            CopyTo(To, From);
        }
    }
}
