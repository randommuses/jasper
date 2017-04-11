using System;
using System.Linq;
using Baseline;
using JasperBus.Configuration;
using JasperBus.Marten.Tests.Setup;
using JasperBus.Runtime.Subscriptions;
using JasperBus.Tests.Runtime.Subscriptions;
using Marten;
using Xunit;
using Shouldly;

namespace JasperBus.Marten.Tests
{
    public class MartenSubscriptionRepositoryTests : IDisposable
    {
        private readonly DocumentStore _documentStore;
        private readonly MartenSubscriptionRepository _repository;
        private readonly SubscriptionCache _cache = new SubscriptionCache();

        public MartenSubscriptionRepositoryTests()
        {
            _documentStore = DocumentStore.For(_ =>
            {
                _.Connection(ConnectionSource.ConnectionString);
                _.AutoCreateSchemaObjects = AutoCreate.All;
            });

            _documentStore.Advanced.Clean.CompletelyRemoveAll();

            _repository = new MartenSubscriptionRepository(
                new ChannelGraph {Name = "TheNode"},
                new MartenSubscriptionSettings {PollingIntervalSeconds = 0.25},
                _cache,
                _documentStore);
        }

        [Fact]
        public void persists_subscriptions()
        {
            var subscriptions = new[]
            {
                Subs.ExistingSubscription(),
                Subs.ExistingSubscription()
            };

            _repository.PersistSubscriptions(subscriptions);

            var loadedSubscriptions = _repository.LoadSubscriptions(SubscriptionRole.Subscribes);
            loadedSubscriptions.ShouldHaveTheSameElementsAs(subscriptions);
        }

        [Fact]
        public void loads_subscriptions()
        {
            var subscriptions = new[]
            {
                Subs.ExistingSubscription(),
                Subs.ExistingSubscription()
            };
            using (var session = _documentStore.LightweightSession())
            {
                subscriptions.Each(x => session.Store(x));
                session.SaveChanges();
            }

            var loadedSubscriptions = _repository.LoadSubscriptions(SubscriptionRole.Subscribes);
            loadedSubscriptions.ShouldHaveTheSameElementsAs(subscriptions);
        }

        [Fact]
        public void removes_subscriptions()
        {
            var subscriptions = new[]
            {
                Subs.ExistingSubscription(),
                Subs.ExistingSubscription(),
                Subs.ExistingSubscription(),
                Subs.ExistingSubscription()
            };
            using (var session = _documentStore.LightweightSession())
            {
                subscriptions.Each(x => session.Store(x));
                session.SaveChanges();
            }
            _repository.RemoveSubscriptions(subscriptions.Skip(1).Take(2));
            var loadedSubscriptions = _repository.LoadSubscriptions(SubscriptionRole.Subscribes);
            loadedSubscriptions.ShouldHaveTheSameElementsAs(subscriptions.First(), subscriptions.Last());
        }

        public void Dispose()
        {
            _documentStore?.Dispose();
            _repository?.Dispose();
        }
    }
}
