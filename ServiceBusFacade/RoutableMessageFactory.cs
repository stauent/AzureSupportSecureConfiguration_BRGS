using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ServiceBusFacade
{
    public static class RoutableMessageFactory
    {
        public static IRoutableMessage CreateMessage<T>(T message, string sender, string recipient) where T : class
        {
            string data = JsonConvert.SerializeObject(message);
            IRoutableMessage msg = new RoutableMessage(data, typeof(T).ToString(), sender, recipient);
            return (msg);
        }

        public static IRoutableMessage BusTopicMessage(this object message, string sender, string recipient)
        {
            string data = JsonConvert.SerializeObject(message);
            IRoutableMessage msg = new RoutableMessage(data, message.GetType().ToString(), sender, recipient);
            return (msg);
        }

        public static async Task Publish(this object message, IMessageBus bus, string sender, string recipient)
        {
            IRoutableMessage msg = message.BusTopicMessage(sender, recipient);
            await bus.Publish(msg);
        }

        public static IRoutableMessage MessageFromBus(this string message)
        {
            IRoutableMessage msg = JsonConvert.DeserializeObject<RoutableMessage>(message);
            return (msg);
        }
    }

}
