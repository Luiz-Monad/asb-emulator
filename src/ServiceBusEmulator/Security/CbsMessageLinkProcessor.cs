using System;
using System.Threading.Channels;
using System.Threading.Tasks;
using Amqp;
using Amqp.Framing;
using Amqp.Listener;
using Microsoft.Extensions.Logging;

namespace ServiceBusEmulator.Security
{
    public class CbsMessageLinkProcessor : ILinkProcessor
    {
        private readonly ILogger _logger;
        private readonly CbsRequestProcessor _cbsRequestProcessor;
        private readonly Channel<Message> messages = Channel.CreateUnbounded<Message>();
        private ILinkProcessor linkProcessor;
        private ListenerLink requestLink;
        private ListenerLink responseLink;
        private uint requestHandle;
        private uint responseHandle;

        public int Credit => 100;

        public CbsMessageLinkProcessor(ILogger<CbsMessageLinkProcessor> logger, CbsRequestProcessor cbsRequestProcessor)
        {
            _logger = logger;
            _cbsRequestProcessor = cbsRequestProcessor;
        }

        public void Process(AttachContext attachContext)
        {
            ListenerLink link = attachContext.Link;
            Attach attach = attachContext.Attach;
            string address = attach.Role ? ((Source)attach.Source).Address : ((Target)attach.Target).Address;
            if (address != "$cbs" && linkProcessor != null)
            {
                linkProcessor.Process(attachContext);
                return;
            }
            _logger.LogDebug($"$cbs attach { (attach.Role ? "source" : "target") } : {link.Name}");
            if (!link.Role)
            {
                requestLink = link;
                requestHandle = attach.Handle;
                link.InitializeSender((c, p, s) => { }, null, this);
                link.AddClosedCallback(OnLinkClosed);
            }
            else
            {
                responseLink = link;
                responseHandle = attach.Handle;
                link.InitializeReceiver((uint)_cbsRequestProcessor.Credit, DispatchRequest, this);
                link.AddClosedCallback(OnLinkClosed);
            }
            link.CompleteAttach(attach, null);
        }

        static void DispatchRequest(ListenerLink link, Message message, DeliveryState deliveryState, object state)
        {
            CbsMessageLinkProcessor thisPtr = (CbsMessageLinkProcessor)state;
            thisPtr._logger.LogDebug($"$cbs dispatch processor req: {message.Body}");

            Outcome outcome;
            if (thisPtr.responseLink == null)
            {
                outcome = new Rejected() {
                    Error = new Error(ErrorCode.NotFound) {
                        Description = "Not response link was found. Ensure the link is attached or reply-to is set on the request."
                    }
                };
            }
            else
            {
                outcome = new Accepted();
            }

            link.DisposeMessage(message, outcome, true);

            thisPtr._cbsRequestProcessor.Process(message, thisPtr.requestLink, thisPtr.responseLink, (message) =>
            {
                thisPtr._logger.LogDebug($"$cbs dispatch processor resp: {message.ApplicationProperties.Map}");
                thisPtr.requestLink.SendMessage(message);
            });
        }

        static void OnLinkClosed(IAmqpObject sender, Error error)
        {
            ListenerLink link = (ListenerLink)sender;
            CbsMessageLinkProcessor thisPtr = (CbsMessageLinkProcessor)link.State;
            if (!link.Role)
            {
                thisPtr.responseLink = null;
            }
            else
            {
                thisPtr.requestLink = null;
            }
            link.Closed -= OnLinkClosed;
        }

        /// <summary>
        /// Registers a link processor to handle received attach performatives.
        /// </summary>
        /// <param name="linkProcessor">The link processor to be registered.</param>
        public void RegisterPreviousLinkProcessor(ILinkProcessor linkProcessor)
        {
            if (this.linkProcessor != null)
            {
                throw new AmqpException(ErrorCode.NotAllowed, this.linkProcessor.GetType().Name + " already registered");
            }

            this.linkProcessor = linkProcessor;
        }
    }
}
