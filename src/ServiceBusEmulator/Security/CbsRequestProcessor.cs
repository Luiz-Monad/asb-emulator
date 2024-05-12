using Amqp;
using Amqp.Framing;
using Amqp.Listener;
using Microsoft.Extensions.Logging;
using ServiceBusEmulator.Abstractions.Security;
using System;

namespace ServiceBusEmulator.Security
{
    public class CbsRequestProcessor
    {
        private readonly ISecurityContext _messageContext;
        private readonly ILogger _logger;
        private readonly ITokenValidator _tokenValidator;

        public int Credit => 100;

        public CbsRequestProcessor(ISecurityContext messageContext, ILogger<CbsRequestProcessor> logger, ITokenValidator tokenValidator)
        {
            _messageContext = messageContext;
            _logger = logger;
            _tokenValidator = tokenValidator;
        }

        public void Process(Message message, ListenerLink reqLink, ListenerLink respLink, Action<Message> complete)
        {
            if (ValidateCbsRequest(message))
            {
                _messageContext.Authorize(reqLink.Session.Connection);
                using Message response = GetResponseMessage(200, message);
                complete(response);
            }
            else
            {
                using (Message response = GetResponseMessage(401, message))
                {
                    complete(response);
                }
                respLink.Close();
                respLink.AddClosedCallback((sender, _) => ((Link)sender).Session.Connection.CloseAsync());
            }
        }

        private bool ValidateCbsRequest(Message message)
        {
            string token = (string)message.Body;
            try
            {
                _tokenValidator.Validate(token);
                _logger.LogDebug($"Valid $cbs request; {token}.");
                return true;
            }
            catch (ArgumentException e)
            {
                _logger.LogError(e, $"Failed to validate $cbs request; {token}.");
                return false;
            }
        }

        private static Message GetResponseMessage(int responseCode, Message message)
        {
            // python SDK $cbs authentication uses integer message IDs.
            Message response = new Message
            {
                Properties = new Properties(),
                ApplicationProperties = new ApplicationProperties
                {
                    ["status-code"] = responseCode
                }
            };
            response.Properties.SetCorrelationId(message.Properties.GetMessageId());
            return response;
        }
    }
}
