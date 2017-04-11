using System;
using System.Linq;
using JasperBus.Configuration;

namespace JasperBus.Runtime.Subscriptions
{
    public class TransportNode
    {
        public TransportNode(ChannelGraph graph)
        {
            NodeName = graph.Name;
            Address = graph.ControlChannel?.Uri ?? graph.FirstOrDefault(x => x.Incoming)?.Uri;
            MachineName = Environment.MachineName;
            Id = $"{NodeName}@{MachineName}";
        }

        public string NodeName { get; set; }
        public string Id { get; set; }
        public string MachineName { get; set; }
        public Uri Address { get; set; }

        public override string ToString()
        {
            return $"Id: {Id}, MachineName: {MachineName}, NodeName: {NodeName}";
        }
    }
}
