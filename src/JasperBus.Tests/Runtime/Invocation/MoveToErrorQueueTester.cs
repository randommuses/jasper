﻿using System;
using System.Threading.Tasks;
using JasperBus.ErrorHandling;
using JasperBus.Runtime;
using JasperBus.Runtime.Invocation;
using NSubstitute;
using Xunit;

namespace JasperBus.Tests.Runtime.Invocation
{
    public class MoveToErrorQueueTester
    {
        private Exception theException = new DivideByZeroException();
        private MoveToErrorQueue theContinuation;
        private Envelope theEnvelope = ObjectMother.Envelope();
        private IEnvelopeContext theContext = Substitute.For<IEnvelopeContext>();

        public MoveToErrorQueueTester()
        {
            theContinuation = new MoveToErrorQueue(theException);

            
        }

        [Fact]
        public async Task should_mark_the_envelope_as_failed()
        {
            await theContinuation.Execute(theEnvelope, theContext, DateTime.UtcNow);

            theEnvelope.Callback.Received().MoveToErrors(new ErrorReport(theEnvelope, theException));
        }

        [Fact]
        public async Task should_send_a_failure_ack()
        {
            await theContinuation.Execute(theEnvelope, theContext, DateTime.UtcNow);

            theContext.Received().SendFailureAcknowledgement(theEnvelope, $"Moved message {theEnvelope.CorrelationId} to the Error Queue.\n{theException}");
        }
    }
}