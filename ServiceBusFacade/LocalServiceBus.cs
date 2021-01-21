using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ConfigurationAssistant;

namespace ServiceBusFacade
{
    public class LocalServiceBus : ServiceBusBase, IMessageBus
    {
        public LocalServiceBus(IApplicationSecrets applicationSecrets) : base(applicationSecrets)
        {
        }

        public async Task Publish(IRoutableMessage message)
        {
            message.TraceInformation("Local service bus publish not implemented yet");
        }

        public async Task Subscribe(string endpointName, Action<IRoutableMessage> MessageHandler, Func<Exception, Task> ErrorHandler)
        {
            endpointName.TraceInformation("Local service bus subscription not implemented yet");
        }
    }

}
