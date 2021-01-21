using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceBusFacade
{
    public interface IRoutableMessage
    {
        /// <summary>
        /// Specifies the client sending the message
        /// </summary>
        string Sender { get; set; }

        /// <summary>
        /// Message recipient
        /// </summary>
        string Recipient { get; set; }

        /// <summary>
        /// This property provides metadata about the payload. This helps
        /// recreate the object at the receiver.
        /// </summary>
        string PayloadType { get; set; }

        /// <summary>
        /// Unique messsage identitier. Response message must include the same 
        /// CorrelationId that it received. This is how two parties can communicate
        /// asynchronously about the same message.
        /// </summary>
        Guid CorrelationId { get; }

        /// <summary>
        /// Specifies the time the message is sent
        /// </summary>
        DateTime TimeSent { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T">Type of object being used for the payload content</typeparam>
        /// <param name="payloadContent">object containing the message payload content</param>
        /// <returns></returns>
        IRoutableMessage CreateResponse<T>(T payloadContent) where T : class;

        /// <summary>
        /// Helper method used to create a response message to the original sender.
        /// The sender, recipient and CorrelationId are properly set.
        /// </summary>
        /// <param name="payloadContent">content of message payload</param>
        /// <param name="payloadType">specifies the type of payload</param>
        /// <returns>Fully formed RoutableMessage ready to send</returns>
        IRoutableMessage CreateResponse(string payloadContent, string payloadType);

        /// <summary>
        /// Helper method used to create a response message to the original sender.
        /// The sender, recipient and CorrelationId are properly set.
        /// </summary>
        /// <param name="payloadContent">content of message payload</param>
        /// <param name="payloadType">specifies the type of payload</param>
        /// <returns>Fully formed RoutableMessage ready to send</returns>
        IRoutableMessage CreateResponse(byte[] payloadContent, string payloadType);

        /// <summary>
        /// In order to support binary payloads (like a zip file).
        /// </summary>
        /// <param name="payloadContent">The message payload you want to send as a sequence of bytes</param>
        /// <param name="payloadType">Specifies the raw type of the payload</param>
        void SetPayload(byte[] payloadContent, string payloadType);

        /// <summary>
        /// Sets the payload to the specified parameter contents
        /// </summary>
        /// <param name="payloadContent">The message payload you want to send</param>
        /// <param name="payloadType">Specifies the raw type of the payload</param>
        void SetPayload(string payloadContent, string payloadType);

        /// <summary>
        /// Method used to simplify setting the message payload. The supplied string
        /// is Base64 encoded automatically when set, and decoded when retrieved.
        /// </summary>
        public string GetPayload();

        public T GetPayload<T>();


        string ToString();
    }


}
