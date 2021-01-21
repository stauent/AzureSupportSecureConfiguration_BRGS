using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Azure;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceBusFacade
{
    /// <summary>
    /// This class represents a routable message. It specifies a sender, recipient
    /// a payload and various other properties that allow the parties to communicate about 
    /// a topic asynchronously.
    /// </summary>
    public class RoutableMessage : IRoutableMessage
    {
        public RoutableMessage()
        {
        }

        public RoutableMessage(string payloadContent, string payloadType, string sender, string recipient)
        {
            SetPayload(payloadContent, payloadType);
            Sender = sender;
            Recipient = recipient;
        }
        public RoutableMessage(byte[] payloadContent, string payloadType, string sender, string recipient)
        {
            SetPayload(payloadContent, payloadType);
            Sender = sender;
            Recipient = recipient;
        }

        /// <summary>
        /// Specifies the client sending the message
        /// </summary>
        public string Sender { get; set; }

        /// <summary>
        /// Message recipient
        /// </summary>
        public string Recipient { get; set; }

        /// <summary>
        /// Base64 encoded version of the payload. This prevents problems with firewalls
        /// and double encoding json payloads.
        /// </summary>
        public string _Payload { get; set; }

        /// <summary>
        /// This property provides metadata about the payload. This helps
        /// recreate the object at the receiver.
        /// </summary>
        public string PayloadType { get; set; }

        /// <summary>
        /// Unique messsage identitier. Response message must include the same 
        /// CorrelationId that it received. This is how two parties can communicate
        /// asynchronously about the same message.
        /// </summary>
        public Guid CorrelationId { get; private set; } = Guid.NewGuid();

        /// <summary>
        /// Specifies the time the message is sent
        /// </summary>
        public DateTime TimeSent { get; private set; } = DateTime.Now;

        public IRoutableMessage CreateResponse<T>(T payloadContent) where T : class
        {
            return RoutableMessageFactory.CreateMessage(payloadContent, Recipient, Sender);
        }

        /// <summary>
        /// Helper method used to create a response message to the original sender.
        /// The sender, recipient and CorrelationId are properly set.
        /// </summary>
        /// <param name="payloadContent">content of message payload</param>
        /// <param name="payloadType">specifies the type of payload</param>
        /// <returns>Fully formed RoutableMessage ready to send</returns>
        public IRoutableMessage CreateResponse(string payloadContent, string payloadType)
        {
            RoutableMessage response = new RoutableMessage(payloadContent, payloadType, Recipient, Sender);
            response.CorrelationId = CorrelationId;
            response.TimeSent = DateTime.Now;
            return (response);
        }

        /// <summary>
        /// Helper method used to create a response message to the original sender.
        /// The sender, recipient and CorrelationId are properly set.
        /// </summary>
        /// <param name="payloadContent">content of message payload</param>
        /// <param name="payloadType">specifies the type of payload</param>
        /// <returns>Fully formed RoutableMessage ready to send</returns>
        public IRoutableMessage CreateResponse(byte[] payloadContent, string payloadType)
        {
            RoutableMessage response = new RoutableMessage(payloadContent, payloadType, Recipient, Sender);
            response.CorrelationId = CorrelationId;
            response.TimeSent = DateTime.Now;
            return (response);
        }

        /// <summary>
        /// In order to support binary payloads (like a zip file).
        /// </summary>
        /// <param name="payloadContent">The message payload you want to send as a sequence of bytes</param>
        /// <param name="payloadType">Specifies the raw type of the payload</param>
        public void SetPayload(byte[] payloadContent, string payloadType)
        {
            PayloadType = payloadType;
            _Payload = System.Convert.ToBase64String(payloadContent);
        }

        /// <summary>
        /// Sets the payload to the specified parameter contents
        /// </summary>
        /// <param name="payloadContent">The message payload you want to send</param>
        /// <param name="payloadType">Specifies the raw type of the payload</param>
        public void SetPayload(string payloadContent, string payloadType)
        {
            PayloadType = payloadType;
            _Payload = Base64Encode(payloadContent);
        }

        /// <summary>
        /// Method used to simplify setting the message payload. The supplied string
        /// is Base64 encoded automatically when set, and decoded when retrieved.
        /// </summary>
        public string GetPayload()
        {
            if (_Payload != null)
                return (Base64Decode(_Payload));
            return (null);
        }

        public T GetPayload<T>()
        {
            T retVal = default(T);
            try
            {
                string payload = GetPayload();
                if (payload != null)
                {
                    retVal = JsonConvert.DeserializeObject<T>(payload);
                }
            }
            catch 
            {
            }

            return (retVal);
        }

        /// <summary>
        /// Support method to base 64 encode a string
        /// </summary>
        /// <param name="plainText">Text to encode</param>
        /// <returns>Base64 encoded string</returns>
        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        /// <summary>
        ///  Support method to decode a base64 encoded string
        /// </summary>
        /// <param name="base64EncodedData">base64 encoded string</param>
        /// <returns>Decoded string</returns>
        public static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }

        public override string ToString()
        {
            return (JsonConvert.SerializeObject(this, Formatting.Indented));
        }
    }

}
