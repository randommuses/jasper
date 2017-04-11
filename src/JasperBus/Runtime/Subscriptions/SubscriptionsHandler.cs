using System.Collections.Generic;
using System.Linq;
using Baseline;
using JasperBus.Configuration;
using JasperBus.Transports.LightningQueues;

namespace JasperBus.Runtime.Subscriptions
{
    public class SubscriptionsHandler
    {
        private readonly ChannelGraph _graph;
        private readonly ISubscriptionsRepository _repository;
        private readonly ISubscriptionCache _cache;

        public SubscriptionsHandler(
            ChannelGraph graph,
            ISubscriptionsRepository repository,
            ISubscriptionCache cache)
        {
            _graph = graph;
            _repository = repository;
            _cache = cache;
        }

        public virtual void ReloadSubscriptions()
        {
            var subscriptions = _repository.LoadSubscriptions(SubscriptionRole.Publishes);
            _cache.LoadSubscriptions(subscriptions);
        }

        public void Handle(SubscriptionRequested message)
        {
            var modifiedSubscriptions = message.Subscriptions
                .Select(x =>
                {
                    x.NodeName = _graph.Name;
                    x.Role = SubscriptionRole.Publishes;
                    x.Source = x.Source.ToMachineUri();
                    return x;
                });

            _repository.PersistSubscriptions(modifiedSubscriptions);

            ReloadSubscriptions();
        }
    }

    public class SubscriptionRequested
    {
        private readonly IList<Subscription> _subscriptions = new List<Subscription>();

        public Subscription[] Subscriptions
        {
            get { return _subscriptions.ToArray(); }
            set
            {
                _subscriptions.Clear();
                if (value != null) _subscriptions.AddRange(value);
            }
        }
    }
}
