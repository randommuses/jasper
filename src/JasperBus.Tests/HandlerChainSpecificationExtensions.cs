﻿using System;
using System.Linq;
using System.Linq.Expressions;
using Baseline.Reflection;
using Jasper.Codegen;
using JasperBus.ErrorHandling;
using JasperBus.Model;
using JasperBus.Runtime.Invocation;
using Shouldly;

namespace JasperBus.Tests
{
    public static class HandlerChainSpecificationExtensions
    {
        public static void ShouldHaveHandler<T>(this HandlerChain chain, Expression<Action<T>> expression)
        {
            chain.ShouldNotBeNull();

            var method = ReflectionHelper.GetMethod(expression);
            chain.Handlers.Any(x => x.Method.Name == method.Name).ShouldBeTrue();
        }

        public static void ShouldHaveHandler<T>(this HandlerChain chain, string methodName)
        {
            chain.ShouldNotBeNull();
            chain.Handlers.Any(x => x.Method.Name == methodName && x.HandlerType == typeof(T)).ShouldBeTrue();
        }

        public static void ShouldNotHaveHandler<T>(this HandlerChain chain, Expression<Action<T>> expression)
        {
            if (chain == null) return;

            var method = ReflectionHelper.GetMethod(expression);
            chain.Handlers.Any(x => x.Method.Name == method.Name && x.HandlerType == typeof(T)).ShouldBeFalse();
        }

        public static void ShouldNotHaveHandler<T>(this HandlerChain chain, string methodName)
        {
            chain?.Handlers.Any(x => x.Method.Name == methodName).ShouldBeFalse();
        }

        public static void ShouldBeWrappedWith<T>(this HandlerChain chain) where T : Frame
        {
            chain.ShouldNotBeNull();
            chain.Wrappers.OfType<T>().Any().ShouldBeTrue();
        }

        public static void ShouldHandleExceptionWith<TEx, TContinuation>(this HandlerChain chain)
            where TEx : Exception
            where TContinuation : IContinuation
        {
            chain.ErrorHandlers.OfType<ErrorHandler>()
                .Where(x => x.Conditions.Count() == 1 && x.Conditions.Single() is ExceptionTypeMatch<TEx>)
                .SelectMany(x => x.Sources)
                .OfType<ContinuationSource>()
                .Any(x => x.Continuation is TContinuation)
                .ShouldBeTrue();
        }

        public static void ShouldMoveToErrorQueue<T>(this HandlerChain chain) where T : Exception
        {
            chain.ErrorHandlers.OfType<ErrorHandler>()
                .Where(x => x.Conditions.Count() == 1 && x.Conditions.Single() is ExceptionTypeMatch<T>)
                .SelectMany(x => x.Sources)
                .OfType<MoveToErrorQueueHandler<T>>()
                .Any().ShouldBeTrue();
        }
    }
}