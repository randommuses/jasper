using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Baseline;
using Jasper;
using Jasper.Codegen;
using Jasper.Configuration;
using JasperBus.Configuration;
using JasperBus.Model;
using JasperBus.Runtime;
using JasperBus.Runtime.Invocation;
using JasperBus.Runtime.Serializers;
using JasperBus.Runtime.Subscriptions;
using JasperBus.Transports.LightningQueues;
using StructureMap;
using Policies = JasperBus.Configuration.Policies;

namespace JasperBus
{
    public class ServiceBusFeature : IFeature
    {
        private HandlerGraph _graph;
        public HandlerSource Handlers { get; } = new HandlerSource();

        public GenerationConfig Generation { get; } = new GenerationConfig("JasperBus.Generated");

        public ChannelGraph Channels { get; } = new ChannelGraph();

        public Policies Policies { get; } = new Policies();

        public readonly Registry Services = new ServiceBusRegistry();


        public void Dispose()
        {
            Channels.Dispose();
        }

        Task<Registry> IFeature.Bootstrap(JasperRegistry registry)
        {
            return bootstrap(registry);
        }

        Task IFeature.Activate(JasperRuntime runtime, IGenerationConfig generation)
        {
            return Task.Factory.StartNew(() =>
            {
                var container = runtime.Container;

                // TODO -- will need to be smart enough to do the conglomerate
                // generation config of the base, with service bus specific stuff
                _graph.Compile(generation, container);

                var transports = container.GetAllInstances<ITransport>().ToArray();

                Channels.UseTransports(transports);

                configureSerializationOrder(runtime);

                var pipeline = container.GetInstance<IHandlerPipeline>();

                foreach (var transport in transports)
                {
                    transport.Start(pipeline, Channels);

                    Channels
                        .Where(x => x.Uri.Scheme == transport.Protocol && x.Sender == null)
                        .Each(x =>
                        {
                            x.Sender = new NulloSender(transport, x.Uri);
                        });
                }

                container.GetInstance<INodeDiscovery>().Register(Channels);

                setupSubscriptions(container);
            });
        }

        private void configureSerializationOrder(JasperRuntime runtime)
        {
            var contentTypes = runtime.Container.GetAllInstances<IMessageSerializer>()
                .Select(x => x.ContentType).ToArray();

            var unknown = Channels.AcceptedContentTypes.Where(x => !contentTypes.Contains(x)).ToArray();
            if (unknown.Any())
            {
                throw new UnknownContentTypeException(unknown, contentTypes);
            }

            foreach (var contentType in contentTypes)
            {
                Channels.AcceptedContentTypes.Fill(contentType);
            }
        }

        private void setupSubscriptions(IContainer container)
        {
            var subRepository = container.GetInstance<ISubscriptionsRepository>();
            var subCache = container.GetInstance<ISubscriptionCache>();
            var sender = container.GetInstance<IEnvelopeSender>();

            var staticSubscriptions = container.GetAllInstances<ISubscriptionRequirements>()
                .SelectMany(x => x.DetermineRequirements())
                .Select(x =>
                {
                    x.Id = Guid.NewGuid();
                    x.NodeName = Channels.Name;
                    x.Role = SubscriptionRole.Subscribes;
                    x.Receiver = x.Receiver.ToMachineUri();
                    return x;
                });

            subRepository.PersistSubscriptions(staticSubscriptions);

            sendSubscriptions(subRepository, sender);

            subCache.LoadSubscriptions(subRepository.LoadSubscriptions(SubscriptionRole.Publishes));
        }

        private void sendSubscriptions(ISubscriptionsRepository repository, IEnvelopeSender sender)
        {
            repository.LoadSubscriptions(SubscriptionRole.Subscribes)
                .GroupBy(x => x.Source)
                .Each(group =>
                {
                    var envelope = new Envelope
                    {
                        Message = new SubscriptionRequested
                        {
                            Subscriptions = group.Each(x => x.Source = x.Source.ToMachineUri()).ToArray()
                        },
                        Destination = group.Key
                    };
                    sender.Send(envelope);
                });
        }

        private async Task<Registry> bootstrap(JasperRegistry registry)
        {
            var calls = await Handlers.FindCalls(registry).ConfigureAwait(false);

            _graph = new HandlerGraph();
            _graph.AddRange(calls);

            var subscriptionHandlerType = typeof(SubscriptionsHandler);
            _graph.Add(
                new HandlerCall(
                    subscriptionHandlerType,
                    subscriptionHandlerType.GetMethod("Handle", new[] {typeof(SubscriptionRequested)}))
            );

            _graph.Group();
            Policies.Apply(_graph);

            Services.For<HandlerGraph>().Use(_graph);
            Services.For<ChannelGraph>().Use(Channels);

            if (registry.Logging.UseConsoleLogging)
            {
                Services.For<IBusLogger>().Add<ConsoleBusLogger>();
            }

            return Services;
        }
    }
}
