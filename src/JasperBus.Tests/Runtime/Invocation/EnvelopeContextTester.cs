﻿using System;
using System.Collections.Generic;
using System.Linq;
using Baseline;
using JasperBus.Runtime;
using JasperBus.Runtime.Invocation;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Xunit;

namespace JasperBus.Tests.Runtime.Invocation
{

    public class EnvelopeContextTester
    {

        [Fact]
        public void enqueue()
        {
            var messages = new EnvelopeContext(null, new Envelope{Message = new Message1()}, null);
            var m1 = new Message1();
            var m2 = new Message2();

            messages.EnqueueCascading(m1);
            messages.EnqueueCascading(m2);

            messages.OutgoingMessages().ShouldHaveTheSameElementsAs(m1, m2);
        }

        [Fact]
        public void ignores_nulls_just_fine()
        {
            var messages = new EnvelopeContext(null, new Envelope { Message = new Message1() }, null);
            messages.EnqueueCascading(null);

            messages.OutgoingMessages().Any().ShouldBeFalse();
        }

        [Fact]
        public void enqueue_an_oject_array()
        {
            var messages = new EnvelopeContext(null, new Envelope{Message = new Message1()}, null);
            var m1 = new Message1();
            var m2 = new Message2();

            messages.EnqueueCascading(new object[]{m1, m2});

            messages.OutgoingMessages().ShouldHaveTheSameElementsAs(m1, m2);
        }
    }


    public class when_sending_cascading_messages : InteractionContext<EnvelopeContext>
    {
        [Fact]
        public void swallows_and_logs_exceptions_on_send()
        {
            var original = ObjectMother.Envelope();
            Services.Inject(original);

            // ONLY FOR NOW
            var ex = new NotImplementedException();
            var envelope = ObjectMother.Envelope();

            MockFor<IEnvelopeSender>().Send(envelope).Throws(ex);
            MockFor<IHandlerPipeline>().Logger.Returns(MockFor<IBusLogger>());


            ClassUnderTest.Send(envelope);


            MockFor<IBusLogger>()
                .Received()
                .LogException(ex, envelope.CorrelationId, "Failure while trying to send a cascading message");
        }

        [Fact]
        public void use_envelope_from_the_original_if_not_ISendMyself()
        {
            var original = Substitute.For<Envelope>();
            var message = new Message1();

            var resulting = new Envelope();

            original.ForResponse(message).Returns(resulting);

            ClassUnderTest.SendOutgoingMessage(original, message);

            MockFor<IEnvelopeSender>().Received().Send(resulting);
        }

        [Fact]
        public void use_envelope_from_ISendMySelf()
        {
            var message = Substitute.For<ISendMyself>();
            var original = new Envelope();
            var resulting = new Envelope();

            message.CreateEnvelope(original).Returns(resulting);

            ClassUnderTest.SendOutgoingMessage(original, message);

            MockFor<IEnvelopeSender>().Received().Send(resulting);
        }

        [Fact]
        public void if_original_envelope_is_ack_requested_send_ack_back()
        {
            var original = new Envelope
            {
                ReplyUri = "foo://bar".ToUri(),
                AckRequested = true,
                CorrelationId = Guid.NewGuid().ToString(),
            };

            ClassUnderTest.SendOutgoingMessages(original, new object[0]);


            var envelope = MockFor<IEnvelopeSender>().ReceivedCalls().First().GetArguments()[0].As<Envelope>();

            envelope.ShouldNotBeNull();

            envelope.ResponseId.ShouldBe(original.CorrelationId);
            envelope.Destination.ShouldBe(original.ReplyUri);
            envelope.Message.ShouldBe(new Acknowledgement {CorrelationId = original.CorrelationId});
            envelope.ParentId.ShouldBe(original.CorrelationId);
        }

        [Fact]
        public void do_not_send_ack_if_no_ack_is_requested()
        {
            var original = new Envelope
            {
                ReplyUri = "foo://bar".ToUri(),
                AckRequested = false,
                CorrelationId = Guid.NewGuid().ToString()
            };

            ClassUnderTest.SendOutgoingMessages(original, new object[0]);


            MockFor<IEnvelopeSender>().DidNotReceiveWithAnyArgs().Send(null);
        }

        [Fact]
        public void when_sending_a_failure_ack_if_no_ack_or_response_is_requested_do_nothing()
        {
            var original = new Envelope
            {
                ReplyUri = "foo://bar".ToUri(),
                AckRequested = false,
                ReplyRequested = null,
                CorrelationId = Guid.NewGuid().ToString()
            };

            var recordingSender = new RecordingEnvelopeSender();
            new EnvelopeContext(null, original, recordingSender)
                .SendFailureAcknowledgement(original, "you stink");

            recordingSender.Outgoing.Any()
                .ShouldBeFalse();
        }
    }


    public class when_sending_a_failure_ack_and_ack_is_requested
    {
        private FailureAcknowledgement theAck;
        private Envelope theSentEnvelope;
        private Envelope original;

        public when_sending_a_failure_ack_and_ack_is_requested()
        {
            original = new Envelope
            {
                ReplyUri = "foo://bar".ToUri(),
                AckRequested = true,
                CorrelationId = Guid.NewGuid().ToString()
            };

            var recordingSender = new RecordingEnvelopeSender();
            new EnvelopeContext(null, original, recordingSender)
                .SendFailureAcknowledgement(original, "you stink");

            theSentEnvelope = recordingSender.Sent.Single();
            theAck = theSentEnvelope.Message as FailureAcknowledgement;
        }

        [Fact]
        public void should_have_sent_a_failure_ack()
        {
            theAck.ShouldNotBeNull();
        }

        [Fact]
        public void the_message_should_be_what_was_requested()
        {
            theAck.Message.ShouldBe("you stink");
        }

        [Fact]
        public void should_have_The_parent_id_set_to_the_original_id_for_tracking()
        {
            theSentEnvelope.ParentId.ShouldBe(original.CorrelationId);
        }

        [Fact]
        public void should_have_The_correlation_id_from_the_original_envelope()
        {
            theAck.CorrelationId.ShouldBe(original.CorrelationId);
        }

        [Fact]
        public void should_be_sent_back_to_the_requester()
        {
            theSentEnvelope.Destination.ShouldBe(original.ReplyUri);
        }

        [Fact]
        public void the_response_id_going_back_should_be_the_original_correlation_id()
        {
            theSentEnvelope.ResponseId.ShouldBe(original.CorrelationId);
        }
    }



    public class when_sending_a_failure_ack_and_response_is_requested
    {
        private FailureAcknowledgement theAck;
        private Envelope theSentEnvelope;
        private Envelope original;

        public when_sending_a_failure_ack_and_response_is_requested()
        {
            original = new Envelope
            {
                ReplyUri = "foo://bar".ToUri(),
                AckRequested = false,
                ReplyRequested = "Message1",
                CorrelationId = Guid.NewGuid().ToString()
            };

            var recordingSender = new RecordingEnvelopeSender();


            new EnvelopeContext(null, original, recordingSender)
                .SendFailureAcknowledgement(original, "you stink");

            theSentEnvelope = recordingSender.Sent.Single();
            theAck = theSentEnvelope.Message as FailureAcknowledgement;
        }

        [Fact]
        public void should_have_sent_a_failure_ack()
        {
            theAck.ShouldNotBeNull();
        }

        [Fact]
        public void the_message_should_be_what_was_requested()
        {
            theAck.Message.ShouldBe("you stink");
        }

        [Fact]
        public void should_have_The_correlation_id_from_the_original_envelope()
        {
            theAck.CorrelationId.ShouldBe(original.CorrelationId);
        }

        [Fact]
        public void should_be_sent_back_to_the_requester()
        {
            theSentEnvelope.Destination.ShouldBe(original.ReplyUri);
        }

        [Fact]
        public void the_response_id_going_back_should_be_the_original_correlation_id()
        {
            theSentEnvelope.ResponseId.ShouldBe(original.CorrelationId);
        }
    }

    public class RecordingEnvelopeSender : IEnvelopeSender
    {
        public readonly IList<Envelope> Sent = new List<Envelope>();
        public readonly IList<object> Outgoing = new List<object>();



        public string Send(Envelope envelope)
        {
            Sent.Add(envelope);

            return envelope.CorrelationId;
        }

        public void SendOutgoingMessages(Envelope original, IEnumerable<object> cascadingMessages)
        {
            Outgoing.AddRange(cascadingMessages);
        }

        public void SendFailureAcknowledgement(Envelope original, string message)
        {
            FailureAcknowledgementMessage = message;
        }

        public string Send(Envelope envelope, IMessageCallback callback)
        {
            Sent.Add(envelope);
            return envelope.CorrelationId;
        }

        public string FailureAcknowledgementMessage { get; set; }
    }
}