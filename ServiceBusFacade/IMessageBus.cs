using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;

namespace ServiceBusFacade
{
    /// <summary>
    /// This interface is used to publish/subscribe to topics/subscriptions in a message bus
    /// </summary>
    public interface IMessageBus
    {
        /// <summary>
        /// Publishes a message to a message bus. Information in the IRoutable message provide
        /// sender/receiver information
        /// </summary>
        /// <param name="message">IRoutableMessage containing all information needed to send a message to a message bus topic</param>
        /// <returns>Task</returns>
        Task Publish(IRoutableMessage message);

        /// <summary>
        /// Subscribes to the messages arriving from the service bus. Information about which subscription to listen
        /// to is provided in the endpointName
        /// </summary>
        /// <param name="endpointName">Configuration property used to gather subscription information</param>
        /// <param name="MessageHandler">Specifies the delegate that will handle messages arriving from your subscription</param>
        /// <param name="ErrorHandler">Specifies the delegate that will handle error messages arriving from your subscription</param>
        /// <returns>Task</returns>
        Task Subscribe(string endpointName, Action<IRoutableMessage> MessageHandler, Func<Exception, Task> ErrorHandler);
    }

}
