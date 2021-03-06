﻿using System.Threading.Tasks;
using JasperBus.Tests.Runtime;
using Shouldly;
using Xunit;

namespace JasperBus.Tests.Compilation
{
    [Collection("compilation")]
    public class using_services_from_container : CompilationContext<ServiceUsingHandler>
    {
        public using_services_from_container()
        {
            ServiceUsingHandler.Clear();

            services.AddService<IFakeService, FakeService>();
            services.AddService<IWidget, Widget>();
        }

        [Fact]
        public async Task take_in_one_service()
        {
            var message = new Message1();
            await Execute(message);

            ServiceUsingHandler.LastMessage1.ShouldBeSameAs(message);
            ServiceUsingHandler.LastWidget.ShouldNotBeNull();
        }

        [Fact]
        public async Task take_in_multiple_services()
        {
            var message = new Message2();
            await Execute(message);

            ServiceUsingHandler.LastMessage2.ShouldBeSameAs(message);
            ServiceUsingHandler.LastWidget.ShouldNotBeNull();
            ServiceUsingHandler.LastService.ShouldNotBeNull();
        }
    }


    public class ServiceUsingHandler
    {
        public static void Clear()
        {
            LastService = null;
            LastMessage1 = null;
            LastMessage2 = null;
            LastWidget = null;
        }

        public void Handle(Message1 message, IWidget widget)
        {
            LastWidget = widget;
            LastMessage1 = message;
        }

        public void Handler(Message2 message, IWidget widget, IFakeService service)
        {
            LastWidget = widget;
            LastMessage2 = message;
            LastService = service;

        }

        public static IFakeService LastService { get; set; }

        public static Message1 LastMessage1 { get; set; }
        public static Message2 LastMessage2 { get; set; }

        public static IWidget LastWidget { get; set; }
    }

    public interface IFakeService
    {

    }

    public interface IWidget
    {

    }

    public class FakeService : IFakeService{}
    public class Widget : IWidget{}
}