using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;

namespace HttpMessageHandlerFactory.Implementations
{
    /// <summary>
    /// 已过期的条目
    /// https://github.com/dotnet/runtime/blob/v7.0.0/src/libraries/Microsoft.Extensions.Http/src/ExpiredHandlerTrackingEntry.cs
    /// </summary>
    sealed class ExpiredHandlerEntry
    {
        private readonly WeakReference livenessTracker;

        public bool CanDispose => !livenessTracker.IsAlive;

        public NameProxy NameProxy { get; }

        public IServiceScope ServiceScope { get; }

        /// <summary>
        /// LifetimeHttpHandler的InnerHandler
        /// </summary>
        public HttpMessageHandler InnerHandler { get; }

        /// <summary>
        /// 已过期的条目
        /// 这里不要引用entry.LifetimeHttpHandler 
        /// </summary>
        /// <param name="entry"></param>   
        public ExpiredHandlerEntry(ActiveHandlerEntry entry)
        {
            this.NameProxy = entry.NameProxy;
            this.ServiceScope = entry.ServiceScope;

            this.livenessTracker = new WeakReference(entry.LifetimeHttpHandler);
            this.InnerHandler = entry.LifetimeHttpHandler.InnerHandler!;
        }
    }
}
