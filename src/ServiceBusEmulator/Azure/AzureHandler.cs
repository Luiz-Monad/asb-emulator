using Amqp;
using Amqp.Framing;
using Amqp.Handler;
using Amqp.Listener;
using Amqp.Types;
using ServiceBusEmulator.Security;
using System;

namespace ServiceBusEmulator.Azure
{
    internal sealed class AzureHandler : IHandler
    {
        public static AzureHandler Instance { get; } = new AzureHandler();

        private AzureHandler() { }

        public bool CanHandle(EventId id)
        {
            return id switch {
                EventId.SendDelivery => true,
                EventId.LinkLocalOpen => true,
                EventId.ConnectionLocalOpen => true,
                EventId.SessionLocalOpen => true,
                _ => false,
            };
        }

        public void Handle(Event protocolEvent)
        {
            if (protocolEvent.Id == EventId.SendDelivery && protocolEvent.Context is IDelivery send)
            {
                send.Tag = Guid.NewGuid().ToByteArray();
            }

            if (protocolEvent.Id == EventId.LinkLocalOpen && protocolEvent.Context is Attach attach)
            {
                attach.MaxMessageSize = int.MaxValue;
                attach.Properties = new Fields();
            }

            if (protocolEvent.Id == EventId.SessionLocalOpen && protocolEvent.Context is Begin begin)
            {
                begin.Properties = new Fields();
            }

            if (protocolEvent.Id == EventId.ConnectionLocalOpen && protocolEvent.Context is Open open)
            {
                open.Properties = new Fields();
            }
        }
    }
}
