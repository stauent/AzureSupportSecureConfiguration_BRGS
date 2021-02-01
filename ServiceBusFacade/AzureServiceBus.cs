// The other namespace that can be used for ServiceBus is "Azure.Messaging.ServiceBus"
using ConfigurationAssistant;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Newtonsoft.Json.Linq;
using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic.CompilerServices;

namespace ServiceBusFacade
{
    public class AzureServiceBus: ServiceBusBase, IMessageBus
    {
        // Configuration property names used to get the service bus top and subscription values
        private readonly string Topic = "Topic";
        private readonly string Subscription = "Subscription";

        public AzureServiceBus(IApplicationSecrets applicationSecrets) : base(applicationSecrets)
        {
        }

        public async Task Publish(IRoutableMessage message)
        {
            try
            {
                // Get the connection string from the destination address in the message
                IApplicationSecretsConnectionStrings publisher = _applicationSecrets.Secret(message.Recipient);

                // Create a sender for the topic
                string topicName = publisher[Topic];
                string connectionString = publisher.Value;
                Message serviceBusMessage = new Message(Encoding.UTF8.GetBytes(message.ToString()));

                ISenderClient topicClient = new TopicClient(connectionString, topicName);
                await topicClient.SendAsync(serviceBusMessage);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public async Task Subscribe(string endpointName, Action<IRoutableMessage> MessageHandler, Func<Exception, Task> ErrorHandler)
        {
            // Get the subscription information from the endpointName setting in the configuration file
            IApplicationSecretsConnectionStrings subscriber = _applicationSecrets.Secret(endpointName);
            string connectionString = subscriber.Value;

            string topicName = subscriber[Topic];
            string subscriptionName = subscriber[Subscription];

            string path = EntityNameHelper.FormatSubscriptionPath(topicName, subscriptionName);
            var receiver = new MessageReceiver(connectionString, path);

            receiver.RegisterMessageHandler(
            async (message, cancellationToken) =>
            {
                await receiver.CompleteAsync(message.SystemProperties.LockToken);
                string body = Encoding.UTF8.GetString(message.Body);
                IRoutableMessage msg = body.MessageFromBus();
                MessageHandler(msg);
            },
            new MessageHandlerOptions(e => ErrorHandler(e.Exception))
            {
                AutoComplete = false,
                MaxConcurrentCalls = 2
            });
        }
    }
}
