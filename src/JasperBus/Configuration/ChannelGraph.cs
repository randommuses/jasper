﻿using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Baseline;
using JasperBus.Runtime;
using JasperBus.Runtime.Serializers;

namespace JasperBus.Configuration
{
    public class ChannelGraph : IContentTypeAware, IDisposable, IEnumerable<ChannelNode>
    {
        private readonly ConcurrentDictionary<Uri, ChannelNode> _nodes = new ConcurrentDictionary<Uri, ChannelNode>();

        public readonly List<string> AcceptedContentTypes = new List<string>();
        IEnumerable<string> IContentTypeAware.Accepts => AcceptedContentTypes;
        public string DefaultContentType => AcceptedContentTypes.FirstOrDefault();

        /// <summary>
        /// Used to identify the instance of the running Jasper node
        /// </summary>
        public string Name { get; set; }

        // TODO -- need to make this the default reply channel
        // if it is not explicitly set
        public ChannelNode ControlChannel { get; set; }

        public ChannelGraph()
        {
        }

        // For testing
        public ChannelGraph(params ITransport[] transports)
        {
            UseTransports(transports);
        }

        internal void UseTransports(IEnumerable<ITransport> transports)
        {
            foreach (var transport in transports)
            {
                _transports.SmartAdd(transport.Protocol, transport);
            }
        }

        private readonly IDictionary<string, ITransport> _transports = new Dictionary<string, ITransport>();

        public ChannelNode this[Uri uri]
        {
            get
            {
                return _nodes.GetOrAdd(uri, key => new ChannelNode(uri));
            }
        }

        public ChannelNode this[string uriString] => this[uriString.ToUri()];

        public ChannelNode AddChannelIfMissing(Uri uri)
        {
            return this[uri];
        }

        public bool HasChannel(Uri uri)
        {
            return _nodes.ContainsKey(uri);
        }

        public void Dispose()
        {
            foreach (var transport in _transports.Values)
            {
                transport.Dispose();
            }

            _transports.Clear();

            _nodes.Clear();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<ChannelNode> GetEnumerator()
        {
            return _nodes.Values.GetEnumerator();
        }

        public Envelope Send(Envelope envelope, Uri address, IEnvelopeSerializer serializer, IMessageCallback callback = null)
        {

            ITransport transport = null;
            if (_transports.TryGetValue(address.Scheme, out transport))
            {
                var sending = envelope.Clone();

                var channel = TryGetChannel(address);

                // TODO -- look up channel node modifiers if any
                // TODO -- there's a little opportunity here to try to reuse the serialization
                // if you send to more than one channel at a time w/ the same serializer
                if (sending.Data == null || sending.Data.Length == 0)
                {
                    serializer.Serialize(sending, channel);
                }


                sending.AcceptedContentTypes = AcceptedContentTypes.ToArray();
                if (channel != null)
                {
                    sending.Destination = channel.Destination;
                    sending.ReplyUri = channel.ReplyUri;

                    if (callback == null)
                    {
                        channel.Sender.Send(sending.Data, sending.Headers);
                    }
                    else
                    {
                        callback.Send(sending);
                    }
                }
                else
                {
                    sending.Destination = address;
                    sending.ReplyUri = transport.DefaultReplyUri();

                    if (callback == null)
                    {
                        transport.Send(sending.Destination, sending.Data, sending.Headers);
                    }
                    else
                    {
                        callback.Send(sending);
                    }
                }

                return sending;
            }
            else
            {
                throw new InvalidOperationException($"Unrecognized transport scheme '{address.Scheme}'");
            }

        }

        public ChannelNode TryGetChannel(Uri address)
        {
            ChannelNode node = null;
            _nodes.TryGetValue(address, out node);

            return node;
        }

        public IEnumerable<ChannelNode> IncomingChannelsFor(string scheme)
        {
            return _nodes.Values.Where(x => x.Incoming && x.Uri.Scheme == scheme);
        }
    }
}
