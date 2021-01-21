using System.Threading.Tasks;

namespace FileStorageFacade
{
    /// <summary>
    /// Simple interface used to demonstrate how configuration along with dependency injection
    /// can allow us to be very flexible in how we run our code.
    /// </summary>
    public interface IFileStorageFacade
    {
        Task CopyTo(string From, string To);
        Task CopyFrom(string To, string From);
    }
}
