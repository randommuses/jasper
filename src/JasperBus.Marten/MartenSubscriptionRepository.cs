using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Baseline;
using JasperBus.Configuration;
using JasperBus.Runtime.Subscriptions;
using Marten;
using Npgsql;

namespace JasperBus.Marten
{
    public class MartenSubscriptionRepository : ISubscriptionsRepository
    {
        private readonly ChannelGraph _graph;
        private readonly ISubscriptionCache _cache;
        private readonly IDocumentStore _documentStore;
        private readonly Timer _refreshTimer;
        private readonly string _notificationChannelName;

        public MartenSubscriptionRepository(ChannelGraph graph, MartenSubscriptionSettings settings, ISubscriptionCache cache, IDocumentStore documentStore)
        {
            _graph = graph;
            _cache = cache;
            _documentStore = documentStore;
            _notificationChannelName = settings.PostgresNotifyChannelName;
            _refreshTimer = new Timer(
                OnTimerElapsed,
                null,
                TimeSpan.FromSeconds(settings.PollingIntervalSeconds),
                TimeSpan.FromSeconds(settings.PollingIntervalSeconds));
        }

        public void PersistSubscriptions(IEnumerable<Subscription> subscriptions)
        {
            using (var session = _documentStore.LightweightSession())
            using (var command = session.Connection.CreateCommand())
            {
                var existing = session.Query<Subscription>().Where(x => x.NodeName == _graph.Name).ToList();
                var newReqs = subscriptions.Where(x => !existing.Contains(x)).ToList();
                newReqs.Each(x => session.Store(x));
                if (newReqs.Count > 0)
                {
                    command.CommandText = $"NOTIFY {_notificationChannelName}";
                    command.ExecuteNonQuery();
                }
                session.SaveChanges();
            }
        }

        public IEnumerable<Subscription> LoadSubscriptions(SubscriptionRole subscriptionRole)
        {
            using (var session = _documentStore.LightweightSession())
            {
                return session.Query<Subscription>()
                    .Where(x => x.NodeName == _graph.Name && x.Role == subscriptionRole);
            }
        }

        public void RemoveSubscriptions(IEnumerable<Subscription> subscriptions)
        {
            using (var session = _documentStore.LightweightSession())
            {
                subscriptions.Each(sub => session.Delete<Subscription>(sub.Id));
                session.SaveChanges();
            }
        }

        public void Dispose()
        {
            _refreshTimer?.Dispose();
            _documentStore?.Dispose();
        }

        public void RefreshCache()
        {
            _cache.LoadSubscriptions(LoadSubscriptions(SubscriptionRole.Publishes));
        }

        private void OnTimerElapsed(object _)
        {
            using (var session = _documentStore.LightweightSession())
            using(var command = session.Connection.CreateCommand())
            {
                command.CommandText = $"LISTEN {_notificationChannelName}";
                session.Connection.Notification += OnSubscriptionsNotification;
                command.ExecuteNonQuery();
                session.Connection.Notification -= OnSubscriptionsNotification;
            }
        }

        private void OnSubscriptionsNotification(object sender, NpgsqlNotificationEventArgs e)
        {
            RefreshCache();
        }
    }
}
