﻿using System;
using JasperBus.Runtime;
using JasperBus.Runtime.Invocation;

namespace JasperBus.ErrorHandling
{
    // SAMPLE: IErrorHandler
    public interface IErrorHandler
    {
        IContinuation DetermineContinuation(Envelope envelope, Exception ex);
    }
    // ENDSAMPLE
}