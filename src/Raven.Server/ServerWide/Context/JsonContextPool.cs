﻿using System;
using System.Collections.Concurrent;
using Sparrow.Json;
using Sparrow.Utils;

namespace Raven.Server.ServerWide.Context
{
    public abstract class JsonContextPool<T>
        where T : JsonOperationContext
    {
        private readonly ConcurrentStack<T> _contextPool;

        public JsonContextPool()
        {
            _contextPool = new ConcurrentStack<T>();
        }

        public IDisposable AllocateOperationContext(out JsonOperationContext context)
        {
            T ctx;
            var disposable = AllocateOperationContext(out ctx);
            context = ctx;

            return disposable;
        }

        public IDisposable AllocateOperationContext(out T context)
        {
            if (_contextPool.TryPop(out context) == false)
                context = CreateContext();

            return new ReturnRequestContext
            {
                Parent = this,
                Context = context
            };
        }

        protected abstract T CreateContext();

        private class ReturnRequestContext : IDisposable
        {
            public T Context;
            public JsonContextPool<T> Parent;
            public void Dispose()
            {
                Context.Reset();
                Parent._contextPool.Push(Context);
                //TODO: this probably should have low memory handle
                //TODO: need better policies, stats, reporting, etc
                Parent._contextPool.ReduceSizeIfTooBig(4096);
            }
        }

        public void Dispose()
        {
            T result;
            while (_contextPool.TryPop(out result))
            {
                result.Dispose();
            }
        }
    }
}