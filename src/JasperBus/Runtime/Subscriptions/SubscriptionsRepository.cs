using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Baseline;

namespace JasperBus.Runtime.Subscriptions
{
    public interface ISubscriptionsRepository : IDisposable
    {
        void PersistSubscriptions(IEnumerable<Subscription> subscriptions);
        IEnumerable<Subscription> LoadSubscriptions(SubscriptionRole subscriptionRole);
        void RemoveSubscriptions(IEnumerable<Subscription> subscriptions);
    }

    public class SubscriptionsRepository : ISubscriptionsRepository
    {
        private readonly ISubscriptionCache _cache;

        public SubscriptionsRepository(ISubscriptionCache cache)
        {
            _cache = cache;
        }

        public void RemoveSubscriptions(IEnumerable<Subscription> subscriptions)
        {
            subscriptions.Each(sub => _cache.Remove(sub));
        }

        public void Dispose()
        {
        }

        public void PersistSubscriptions(IEnumerable<Subscription> subscriptions)
        {
            _cache.LoadSubscriptions(subscriptions);
        }

        public IEnumerable<Subscription> LoadSubscriptions(SubscriptionRole subscriptionRole)
        {
            return _cache.ActiveSubscriptions.Where(x => x.Role == subscriptionRole);
        }
    }
}
