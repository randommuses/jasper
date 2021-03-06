﻿using System;
using System.Collections.Generic;
using System.Linq;
using JasperBus.Runtime;
using JasperBus.Runtime.Routing;

namespace JasperBus.Configuration
{
    public enum DeliveryMode
    {
        /// <summary>
        /// If supported by the transport, this opts into guaranteed delivery mechanics for this channel
        /// </summary>
        DeliveryGuaranteed,

        /// <summary>
        /// If supported by the transport, this opts into a faster "fire and forget" mechanism for sending and receiving messages. Use this option for control channels.
        /// </summary>
        DeliveryFastWithoutGuarantee
    }

    public class ChannelNode : IContentTypeAware
    {
        public Uri Uri { get; }

        public ChannelNode(Uri uri)
        {
            Uri = uri;
        }

        public readonly List<string> AcceptedContentTypes = new List<string>();

        IEnumerable<string> IContentTypeAware.Accepts => AcceptedContentTypes;
        public bool Incoming { get; set; }
        public string DefaultContentType => AcceptedContentTypes.FirstOrDefault();

        public IList<IRoutingRule> Rules = new List<IRoutingRule>();

        public bool ShouldSendMessage(Type messageType)
        {
            return Rules.Any(x => x.Matches(messageType));
        }

        public Uri ReplyUri { get; set; }
        public Uri Destination { get; set; }

        public ISender Sender { get; set; }
        public DeliveryMode Mode { get; set; } = DeliveryMode.DeliveryGuaranteed;
        public IList<IEnvelopeModifier> Modifiers { get; } = new List<IEnvelopeModifier>();
    }

    // Use a nullo if need be?
    public interface ISender
    {
        // TODO -- change this to take in Envelope, IEnvelopeSender, ChannelNode
        void Send(byte[] data, IDictionary<string, string> headers);
    }

    public class NulloSender : ISender
    {
        private readonly ITransport _transport;
        private readonly Uri _destination;

        public NulloSender(ITransport transport, Uri destination)
        {
            _transport = transport;
            _destination = destination;
        }

        public void Send(byte[] data, IDictionary<string, string> headers)
        {
            _transport.Send(_destination, data, headers);
        }
    }
}