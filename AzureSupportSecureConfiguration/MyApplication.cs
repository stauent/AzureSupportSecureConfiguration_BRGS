using ConfigurationAssistant;
using FileStorageFacade;
using ServiceBusFacade;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AzureSupportSecureConfiguration
{

    public class MyApplication
    {
        private readonly IApplicationRequirements<MyApplication> _requirements;
        private readonly IFileStorageFacade _storage;
        private readonly IMessageBus _messsageBus;
        private List<RegisterClientRequest> requests = new List<RegisterClientRequest>();

        /// <summary>
        /// We use constructor dependency injection to the interfaces we need at runtime
        /// </summary>
        /// <param name="requirements"></param>
        public MyApplication(IApplicationRequirements<MyApplication> requirements, IFileStorageFacade storage, IMessageBus messsageBus)
        {
            _requirements = requirements;
            _storage = storage;
            _messsageBus = messsageBus;

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
            foreach (RegisterClientRequest request in requests)
            {
                await request.Publish(_messsageBus, "RegisterClientPublisher", "RegisterClientSubscriber");
            }

            //// Write a file to storage and then get it back again
            //await _storage.CopyTo("D:\\BGRS\\Junk\\junk.txt", "copyJunk.txt");
            //await _storage.CopyFrom("D:\\BGRS\\Junk\\copyJunk.txt", "copyJunk.txt");

            //bool areEquals = System.IO.File.ReadLines($"D:\\BGRS\\Junk\\junk.txt").SequenceEqual(System.IO.File.ReadLines($"D:\\BGRS\\Junk\\copyJunk.txt"));

            //$"Comparing both files we find that areEquals = {areEquals}".TraceInformation();
            //File.Delete($"D:\\BGRS\\Junk\\copyJunk.txt");
        }
    }
}
