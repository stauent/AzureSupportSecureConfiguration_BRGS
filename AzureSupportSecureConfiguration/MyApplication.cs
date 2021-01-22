using ConfigurationAssistant;
using FileStorageFacade;
using ServiceBusFacade;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using CacheFacade;

namespace AzureSupportSecureConfiguration
{

    public class MyApplication
    {
        private readonly IApplicationRequirements<MyApplication> _requirements;
        private readonly IFileStorageFacade _storage;
        private readonly IMessageBus _messsageBus;
        private readonly IApplicationCache _applicationCache;

        private List<RegisterClientRequest> requests = new List<RegisterClientRequest>();

        /// <summary>
        /// We use constructor dependency injection to the interfaces we need at runtime
        /// </summary>
        /// <param name="requirements"></param>
        public MyApplication(IApplicationRequirements<MyApplication> requirements, IFileStorageFacade storage, IMessageBus messsageBus, IApplicationCache applicationCache)
        {
            _requirements = requirements;
            _storage = storage;
            _messsageBus = messsageBus;
            _applicationCache = applicationCache;

            // Create a bunch of dummy requests to send
            requests.Add(new RegisterClientRequest { ClientName = "Ford", ConsultantName = "Tom Petty" });
            requests.Add(new RegisterClientRequest { ClientName = "Ford", ConsultantName = "Bob Jones" });
            requests.Add(new RegisterClientRequest { ClientName = "IBM ", ConsultantName = "Albert Einstein" });
        }

        /// <summary>
        /// This is the application entry point. 
        /// </summary>
        /// <returns></returns>
        internal async Task Run()
        {
            $"Application Started at {DateTime.UtcNow}".TraceInformation();

            await DoWork();

            $"Application Ended at {DateTime.UtcNow}".TraceInformation();

            Console.WriteLine("PRESS <ENTER> TO EXIT");
            Console.ReadKey();
        }

        /// <summary>
        /// All work is done here
        /// </summary>
        /// <returns></returns>
        internal async Task DoWork()
        {
            // Demonstrate the cache. We are going to ask the cache if 
            // a value exists in the cache. If it does, we use it. If not
            // we populate the key with the value we want. In real
            // life you use a cache to prevent retrieving data from 
            // a resource that is more time consuming (like a remote service or disk).
            // For demo purposes only, we're going to use the in-memory
            // collection that we have pre-loaded as the sample data.

            // When using the Redis cache you'll notice that the
            // first time you run this application, the cache will be populated.
            // BUT on every subsequent run of the application, the data ALWAYS
            // comes from the cache. Contrast this with the "LocalMemoryCache"
            // which is destroyed every time you stop the application. In addition
            // the RedisCache data is available EVERYWHERE in the world now. Go to
            // another computer and run this application, and it too will pull
            // the same data from the RedisCache. This is a persisted/distributed cache.

            RegisterClientRequest request = null;
            List<string> cacheKeys = new List<string> {"Tom", "Bob", "Albert"};
            foreach (string key in cacheKeys)
            {
                request = _applicationCache.GetCachedObject<RegisterClientRequest>(key);
                if(request == null)
                {
                    // This simulates reading the data from some location that takes a lot of time
                    request = (from r in requests where r.ConsultantName.StartsWith(key) select r).FirstOrDefault();
                    request.TraceInformation($"INITIAL Data was read from some time consuming data store using key {key}");

                    // Cache the object so the next time we try to read it, it's already in the cache
                    _applicationCache.CacheObject(request, key);
                }
                else
                {
                    request.TraceInformation($"INITIAL Data was read from cache using key {key}");
                }
            }

            // Now, we should be able to read ALL values directly from cache
            foreach (string key in cacheKeys)
            {
                request = _applicationCache.GetCachedObject<RegisterClientRequest>(key);
                if (request == null)
                {
                    $"SUBSEQUENT Data was read from cache ERROR using key {key}".TraceInformation();
                }
                else
                {
                    request.TraceInformation($"SUBSEQUENT Data was read from cache using key {key}");
                }
            }

            // Publish a bunch of messages to the message bus.
            // Every instance of the "MessageBusClient" app
            // running anywhere in the world will receive a copy of the published message
            foreach (RegisterClientRequest registerClientRequest in requests)
            {
                await registerClientRequest.Publish(_messsageBus, "RegisterClientPublisher", "RegisterClientSubscriber");
            }

            // Write a file to storage and then get it back again
            const string localFolder = @"C:\BGRS\Junk\";

            await _storage.CopyTo($"{localFolder}junk.txt", "copyJunk.txt");
            await _storage.CopyFrom($"{localFolder}copyJunk.txt", "copyJunk.txt");

            bool areEquals = System.IO.File.ReadLines($"{localFolder}junk.txt").SequenceEqual(System.IO.File.ReadLines($"{localFolder}copyJunk.txt"));

            $"Comparing both files we find that areEquals = {areEquals}".TraceInformation();
            File.Delete($"{localFolder}copyJunk.txt");
        }
    }
}
