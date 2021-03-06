﻿using System;
using System.Threading.Tasks;

namespace JasperBus.Runtime.Invocation
{
    public class MessageSucceededContinuation : IContinuation
    {
        public static readonly MessageSucceededContinuation Instance = new MessageSucceededContinuation();

        private MessageSucceededContinuation()
        {

        }

        public Task Execute(Envelope envelope, IEnvelopeContext context, DateTime utcNow)
        {
            try
            {
                context.SendAllQueuedOutgoingMessages();

                envelope.Callback.MarkSuccessful();

                context.Logger.MessageSucceeded(envelope);
            }
            catch (Exception ex)
            {
                context.SendFailureAcknowledgement(envelope, "Sending cascading message failed: " + ex.Message);

                context.Logger.LogException(ex, envelope.CorrelationId, ex.Message);
                context.Logger.MessageFailed(envelope, ex);

                envelope.Callback.MoveToErrors(new ErrorReport(envelope, ex));
            }

            return Task.CompletedTask;
        }

    }
}